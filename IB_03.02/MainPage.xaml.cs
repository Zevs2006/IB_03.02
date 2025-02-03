using System;
using System.Collections.Generic;
using SQLite;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.IO;
using System.Web;


namespace _1232131212212231
{
    public partial class MainPage : ContentPage
    {
        private readonly string dbPath = Path.Combine(FileSystem.AppDataDirectory, "vulnerable.db");
        private HttpListener? _server;
        private SQLiteConnection? db;

        public MainPage()
        {
            InitializeComponent();
            InitDatabase();
            StartServer();
        }

        private void InitDatabase()
        {
            db = new SQLiteConnection(dbPath);
            db.CreateTable<User>();

            if (db.Table<User>().Count() == 0)
            {
                db.Insert(new User { Username = "admin", Password = "password" });
            }
        }

        private string AuthenticateUser(string? username, string? password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return "Ошибка: пустые данные.";

            var user = db?.Table<User>().FirstOrDefault(u => u.Username == username && u.Password == password);
            return user != null ? $"Добро пожаловать, {username}!" : "Ошибка: неверные данные.";
        }

        public class User
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
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
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var body = reader.ReadToEnd();
                var formData = HttpUtility.ParseQueryString(body);

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
            if (string.IsNullOrEmpty(UsernameEntry.Text) || string.IsNullOrEmpty(PasswordEntry.Text))
            {
                ResultLabel.Text = "Введите логин и пароль.";
                return;
            }

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
