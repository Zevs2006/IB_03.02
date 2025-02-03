using System;
using System.Collections.Generic;
using SQLite;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace IB_03._02
{
    public partial class MainPage : ContentPage
    {
        private readonly string dbPath = "vulnerable.db";
        private HttpListener _server;

        public MainPage()
        {
            InitializeComponent();
            InitDatabase();
            StartServer();
        }

        private SQLiteConnection db;

        private void InitDatabase()
        {
            db = new SQLiteConnection(System.IO.Path.Combine(FileSystem.AppDataDirectory, "vulnerable.db"));
            db.CreateTable<User>();

            if (db.Table<User>().Count() == 0)
            {
                db.Insert(new User { Username = "admin", Password = "password" });
            }
        }

        private string AuthenticateUser(string username, string password)
        {
            var user = db.Table<User>().FirstOrDefault(u => u.Username == username && u.Password == password);
            return user != null ? $"Добро пожаловать, {username}!" : "Ошибка: неверные данные.";
        }

        // Модель пользователя
        public class User
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }


        private async void StartServer()
        {
            _server = new HttpListener();
            _server.Prefixes.Add("http://localhost:5000/");
            _server.Start();

            await Task.Run(() =>
            {
                while (true)
                {
                    var context = _server.GetContext();
                    ProcessRequest(context);
                }
            });
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/login")
            {
                using var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding);
                var body = reader.ReadToEnd();
                var formData = System.Web.HttpUtility.ParseQueryString(body);

                var username = formData["username"];
                var password = formData["password"];

                string result = AuthenticateUser(username, password);
                byte[] buffer = Encoding.UTF8.GetBytes(result);

                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            using var client = new HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", UsernameEntry.Text),
                new KeyValuePair<string, string>("password", PasswordEntry.Text)
            });

            var response = await client.PostAsync("http://localhost:5000/login", content);
            var result = await response.Content.ReadAsStringAsync();

            ResultLabel.Text = result;
        }
    }
}
