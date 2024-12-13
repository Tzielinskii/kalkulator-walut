using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.Net.Mail;

class CurrencyConverter
{
    private static readonly string apiKey = "3c8df9ae8c19e3eb0d6285bb"; // Zamień na swój klucz API
    private static readonly string apiUrl = "https://api.exchangerate-api.com/v4/latest/USD"; // Punkt końcowy API
    private static readonly string ratesFilePath = "exchangeRates.json"; // Ścieżka do pliku z zapisanymi kursami

    static async Task Main(string[] args)
    {
        try
        {
            // Wysyłanie maila po porównaniu kursów
            await SendEmailBasedOnRateChanges();

            // Po wysłaniu e-maila, uruchom kalkulator walutowy
            await StartCurrencyCalculator();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Wystąpił błąd: {ex.Message}");
        }

        // Czekanie na naciśnięcie klawisza, aby aplikacja nie zamknęła się natychmiast
        Console.ReadKey();
    }

    // Metoda do wysyłania e-maila w zależności od zmiany kursów
    private static async Task SendEmailBasedOnRateChanges()
    {
        var rates = await GetExchangeRates();

        if (rates != null)
        {
            // Sprawdzenie, czy plik z poprzednimi kursami istnieje
            if (File.Exists(ratesFilePath))
            {
                // Wczytanie poprzednich kursów
                var previousRates = JsonConvert.DeserializeObject<ExchangeRates>(File.ReadAllText(ratesFilePath));

                // Porównanie kursów
                bool ratesChanged = false;
                foreach (var rate in rates.Rates)
                {
                    if (previousRates.Rates.ContainsKey(rate.Key) && previousRates.Rates[rate.Key] != rate.Value)
                    {
                        ratesChanged = true;
                        break;
                    }
                }

                // Wysyłanie odpowiedniego maila
                if (ratesChanged)
                {
                    await SendEmail("Kursy walut się zmieniły", "Kursy walut zostały zaktualizowane. Sprawdź najnowsze dane.");
                }
                else
                {
                    await SendEmail("Kursy walut się nie zmieniły", "Kursy walut nie uległy zmianie od ostatniego sprawdzenia.");
                }
            }
            else
            {
                // Jeśli plik nie istnieje, zapisujemy aktualne kursy
                await SendEmail("Pierwsze uruchomienie", "Aplikacja została uruchomiona po raz pierwszy. Kursy zostały zapisane.");
            }

            // Zapisz bieżące kursy do pliku
            File.WriteAllText(ratesFilePath, JsonConvert.SerializeObject(rates));
        }
    }

    // Metoda do wysyłania e-maila
    private static async Task SendEmail(string subject, string body)
    {
        try
        {
            string smtpHost = "sandbox.smtp.mailtrap.io"; // Adres SMTP Mailtrap
            int smtpPort = 587; // Port SMTP
            string smtpUser = "371d21418cc204"; // Twój użytkownik z Mailtrap
            string smtpPass = "abaedc794fb7d2"; // Twoje hasło SMTP z Mailtrap

            var mailMessage = new MailMessage
            {
                From = new MailAddress("hultidalmi@gufum.com"), // Adres nadawcy
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            // Odbiorca wiadomości
            mailMessage.To.Add("hultidalmi@gufum.com");

            var smtpClient = new SmtpClient(smtpHost)
            {
                Port = smtpPort,
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            await smtpClient.SendMailAsync(mailMessage);
            Console.WriteLine("Wiadomość została wysłana!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd przy wysyłaniu e-maila: {ex.Message}");
        }
    }

    // Klasa do deserializacji odpowiedzi API
    public class ExchangeRates
    {
        public string Base { get; set; }
        public DateTime Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
    }

    // Metoda pobierająca kursy walut z API
    private static async Task<ExchangeRates> GetExchangeRates()
    {
        using (var client = new HttpClient())
        {
            try
            {
                var response = await client.GetStringAsync(apiUrl);
                return JsonConvert.DeserializeObject<ExchangeRates>(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd przy pobieraniu kursów: {ex.Message}");
                return null;
            }
        }
    }

    // Metoda kalkulatora walutowego
    private static async Task StartCurrencyCalculator()
    {
        Console.WriteLine("Kalkulator walutowy");

        // Pobieranie kursów walut
        var rates = await GetExchangeRates();

        if (rates != null)
        {
            Console.WriteLine("Dostępne waluty:");
            foreach (var rate in rates.Rates)
            {
                Console.WriteLine($"{rate.Key}: {rate.Value}");
            }

            Console.WriteLine("\nPodaj walutę źródłową (np. USD):");
            string fromCurrency = Console.ReadLine().ToUpper();

            Console.WriteLine("Podaj walutę docelową (np. EUR):");
            string toCurrency = Console.ReadLine().ToUpper();

            Console.WriteLine("Podaj kwotę do przeliczenia:");
            if (decimal.TryParse(Console.ReadLine(), out decimal amount))
            {
                if (rates.Rates.ContainsKey(fromCurrency) && rates.Rates.ContainsKey(toCurrency))
                {
                    decimal conversionRate = rates.Rates[toCurrency] / rates.Rates[fromCurrency];
                    decimal result = amount * conversionRate;
                    Console.WriteLine($"{amount} {fromCurrency} = {result:F2} {toCurrency}");
                }
                else
                {
                    Console.WriteLine("Nieprawidłowa waluta. Sprawdź dostępne waluty.");
                }
            }
            else
            {
                Console.WriteLine("Wprowadź prawidłową kwotę.");
            }
        }
        else
        {
            Console.WriteLine("Nie udało się pobrać danych o kursach.");
        }
    }
}