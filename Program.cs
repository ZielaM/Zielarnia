using System;
using Zielarnia.Services;
using Zielarnia.Data;
using Zielarnia.Models.User;
using MySqlConnector;
using Zielarnia.Utils.Exceptions;
using Zielarnia.Data.Repositories;

namespace Zielarnia
{
    class Program
    {
        private static AuthService _authService;
        private static ProductService _productService;
        private static OrderService _orderService;
        private static LoggingService _loggingService;
        private static User _currentUser;

        static void Main(string[] args)
        {
            InitializeServices();

            Console.WriteLine("=== SYSTEM ZIELARNIA ===");

            while (true)
            {
                try
                {
                    if (_currentUser == null)
                    {
                        ShowLoginMenu();
                    }
                    else
                    {
                        ShowMainMenu();
                    }
                }
                catch (DatabaseException ex)
                {
                    Console.WriteLine($"Błąd bazy danych: {ex.Message}");
                    _loggingService.LogError(ex);
                }
                catch (AuthException ex)
                {
                    Console.WriteLine($"Błąd autentykacji: {ex.Message}");
                    _loggingService.LogError(ex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Nieoczekiwany błąd: {ex.Message}");
                    _loggingService.LogError(ex);
                }
            }
        }

        private static void InitializeServices()
        {
            var connectionString = "Server=localhost;Database=zielonazielarnia;User=root;Password=admin;";
            var dbContext = new DatabaseContext(connectionString);

            var userRepository = new UserRepository(dbContext);
            var productRepository = new ProductRepository(dbContext);
            var orderRepository = new OrderRepository(dbContext);

            _authService = new AuthService(userRepository);
            _productService = new ProductService(productRepository);
            _orderService = new OrderService(orderRepository, productRepository);
            _loggingService = new LoggingService(dbContext);

            _authService.UserLoggedIn += (sender, e) =>
            {
                _currentUser = e.User;
                _loggingService.LogAction(_currentUser, $"Zalogowano jako {_currentUser.Role}");
                Console.WriteLine($"Witaj, {_currentUser.Username}!");
            };

            _authService.UserLoggedOut += (sender, e) =>
            {
                _loggingService.LogAction(_currentUser, "Wylogowano");
                _currentUser = null;
                Console.WriteLine("Wylogowano pomyślnie.");
            };
        }

        private static void ShowLoginMenu()
        {
            Console.WriteLine("\n=== MENU LOGOWANIA ===");
            Console.WriteLine("1. Zaloguj się");
            Console.WriteLine("2. Wyjdź");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Console.Write("Login: ");
                    var username = Console.ReadLine();
                    Console.Write("Hasło: ");
                    var password = Console.ReadLine();

                    _currentUser = _authService.Login(username, password);
                    break;
                case "2":
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Nieprawidłowy wybór");
                    break;
            }
        }

        private static void ShowMainMenu()
        {
            Console.WriteLine($"\n=== MENU GŁÓWNE ({_currentUser.Role}) ===");
            Console.WriteLine("1. Przeglądaj produkty");
            Console.WriteLine("2. Złóż zamówienie");
            Console.WriteLine("3. Zobacz swoje zamówienia");
            Console.WriteLine("4. Wyloguj się");

            if (_currentUser is Admin)
            {
                Console.WriteLine("5. Zarządzaj produktami");
                Console.WriteLine("6. Zarządzaj zamówieniami");
                Console.WriteLine("7. Zobacz logi systemowe");
            }

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    _productService.BrowseProducts();
                    break;
                case "2":
                    CreateNewOrder();
                    break;
                case "3":
                    _orderService.ViewCustomerOrders(_currentUser.Id);
                    break;
                case "4":
                    _authService.Logout();
                    break;
                case "5" when _currentUser is Admin:
                    ManageProducts();
                    break;
                case "6" when _currentUser is Admin:
                    _orderService.ManageOrders();
                    break;
                case "7" when _currentUser is Admin:
                    _loggingService.ShowSystemLogs();
                    break;
                default:
                    Console.WriteLine("Nieprawidłowy wybór");
                    break;
            }
        }

        private static void CreateNewOrder()
        {
            Console.WriteLine("\n=== TWORZENIE NOWEGO ZAMÓWIENIA ===");
            _productService.BrowseProducts();

            Console.Write("Podaj ID produktu: ");
            var productId = int.Parse(Console.ReadLine());
            Console.Write("Podaj ilość: ");
            var quantity = int.Parse(Console.ReadLine());

            _orderService.CreateOrder(_currentUser.Id, productId, quantity);
            _loggingService.LogAction(_currentUser, $"Złożono nowe zamówienie produktu ID: {productId}");
        }

        private static void ManageProducts()
        {
            Console.WriteLine("\n=== ZARZĄDZANIE PRODUKTAMI ===");
            Console.WriteLine("1. Dodaj nowy produkt");
            Console.WriteLine("2. Edytuj produkt");
            Console.WriteLine("3. Usuń produkt");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    AddNewProduct();
                    break;
                case "2":
                    // Implementacja edycji produktu
                    break;
                case "3":
                    // Implementacja usuwania produktu
                    break;
                default:
                    Console.WriteLine("Nieprawidłowy wybór");
                    break;
            }
        }

        private static void AddNewProduct()
        {
            Console.WriteLine("\n=== DODAWANIE NOWEGO PRODUKTU ===");
            Console.Write("Nazwa: ");
            var name = Console.ReadLine();
            Console.Write("Opis: ");
            var description = Console.ReadLine();
            Console.Write("Cena: ");
            var price = decimal.Parse(Console.ReadLine());
            Console.Write("ID kategorii: ");
            var categoryId = int.Parse(Console.ReadLine());

            _productService.AddProduct(name, description, price, categoryId);
            _loggingService.LogAction(_currentUser, $"Dodano nowy produkt: {name}");
        }
    }
}