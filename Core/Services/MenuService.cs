using Zielarnia.Core.Models;
using Zielarnia.Core.Models.Enums;
using Zielarnia.Data.Services;
using Zielarnia.UI;
using Zielarnia.Core.Interfaces; // Use ILoggerService
using System; // For Exception
using System.Threading.Tasks; // For Task
using System.Linq; // For Linq methods like Any()
using System.Collections.Generic; // For List<>
using Zielarnia.Data.Models; // For data models like Product, Order etc.

namespace Zielarnia.Core.Services;

/// <summary>
/// Manages the display and interaction logic for console menus.
/// </summary>
public class MenuService
{
    private readonly DatabaseService _dbService;
    private readonly ILoggerService _logger; // Inject logger

    // Inject other services if needed (e.g., OrderService, ProductService might be better than direct DB calls here)
    public MenuService(DatabaseService dbService, ILoggerService logger) // Inject logger
    {
        _dbService = dbService;
        _logger = logger;
    }

    /// <summary>
    /// Shows the main menu loop based on the logged-in user's role.
    /// </summary>
    public async Task ShowMainMenu(User user)
    {
        bool keepRunning = true;
        while (keepRunning)
        {
            Console.Clear();
            ConsoleHelper.DisplayHeader($"Menu Główne ({user.Role})");

            // --- Display Common Options ---
            Console.WriteLine(" 1. Wyświetl produkty");

            // --- Display Role-Specific Options ---
            switch (user.Role)
            {
                case UserRole.Client:
                    DisplayClientOptions();
                    break;
                case UserRole.Herbalist:
                    DisplayHerbalistOptions();
                    break;
                case UserRole.Admin:
                    DisplayAdminOptions(); // Admin might see Herbalist + more
                    break;
            }

             // --- Display Common Exit Option ---
            Console.WriteLine("--------------------");
            Console.WriteLine(" 0. Wyloguj i zakończ");
            Console.WriteLine("--------------------");

            int choice = ConsoleHelper.ReadInt("Wybierz opcję:");

            // Log the chosen action before handling
            await _logger.LogInfoAsync($"User '{user.Login}' chose menu option: {choice} (Role: {user.Role})");

            try // Add top-level try-catch for handling errors during action execution
            {
                 // Handle choice based on role
                switch (user.Role)
                {
                    case UserRole.Client:
                        keepRunning = await HandleClientChoice(choice, user);
                        break;
                    case UserRole.Herbalist:
                        keepRunning = await HandleHerbalistChoice(choice, user);
                        break;
                    case UserRole.Admin:
                        keepRunning = await HandleAdminChoice(choice, user);
                        break;
                }
            }
            catch (Exception ex) // Catch exceptions propagated from action methods or DB service
            {
                 // Error should have been logged by lower layers if possible
                 // Log the context here
                 ConsoleHelper.WriteError($"Wystąpił nieoczekiwany błąd podczas wykonywania akcji: {ex.Message}");
                 await _logger.LogErrorAsync($"Error during menu action execution for user '{user.Login}', choice {choice}. See previous logs for details.", ex);
                 // Decide if the loop should continue or break on error
                 keepRunning = true; // Default to continue unless critical
                 Console.WriteLine("\nNaciśnij dowolny klawisz, aby spróbować ponownie...");
                 Console.ReadKey(); // Pause so user sees error
            }


            // Pause before next iteration unless logging out
            if (keepRunning)
            {
                Console.WriteLine("\nNaciśnij dowolny klawisz, aby wrócić do menu...");
                Console.ReadKey();
            }
        }
        // Log logout action after loop ends
        await _logger.LogInfoAsync($"User '{user.Login}' logged out.");
    }

    // --- Private methods for displaying options ---

    private void DisplayClientOptions()
    {
        Console.WriteLine(" 2. Złóż nowe zamówienie");
        Console.WriteLine(" 3. Wyświetl moje zamówienia");
        // Add more client options if needed
    }

    private void DisplayHerbalistOptions()
    {
        // Herbalist usually has client permissions + more
        // DisplayClientOptions(); // Or specific herbalist view of orders/products
        Console.WriteLine("--- Zarządzanie Zielarnią ---");
        Console.WriteLine("10. Zarządzaj produktami (dodaj/edytuj/usuń)");
        Console.WriteLine("11. Zarządzaj kategoriami");
        Console.WriteLine("12. Zarządzaj klientami");
        Console.WriteLine("13. Zarządzaj zamówieniami (przeglądaj/zmień status)");
        Console.WriteLine("14. Zarządzaj dostawcami");
        Console.WriteLine("15. Zarządzaj dostawami produktów"); // Maybe just view for now?
        Console.WriteLine("16. Wyświetl wszystkie zamówienia");
    }

     private void DisplayAdminOptions()
    {
        // Admin usually has Herbalist permissions + potentially more
        DisplayHerbalistOptions();
        Console.WriteLine("--- Administracja ---");
        Console.WriteLine("20. Zarządzaj typami rachunków");
        Console.WriteLine("21. Zarządzaj rachunkami");
        Console.WriteLine("22. Generuj raporty (np. sprzedaży) [NIEDOSTĘPNE]"); // Placeholder
        // Console.WriteLine("23. Zarządzaj użytkownikami aplikacji"); // Management via file as per requirements
    }

    // --- Private methods for handling choices ---

    private async Task<bool> HandleClientChoice(int choice, User user)
    {
        switch (choice)
        {
            case 1: await ShowAllProducts(user); return true; // Continue running
            case 2: await PlaceNewOrder(user); return true;
            case 3: await ShowMyOrders(user); return true;
            case 0: return false; // Stop running (logout)
            default:
                ConsoleHelper.WriteWarning("Nieznana opcja dla Twojej roli.");
                return true;
        }
    }

    private async Task<bool> HandleHerbalistChoice(int choice, User user)
    {
        // Try handling as Client first for shared options
        if (choice >= 1 && choice <= 3) return await HandleClientChoice(choice, user);
        if (choice == 0) return false; // Logout

        switch(choice)
        {
            // Herbalist specific options
            case 10: await ManageProducts(user); return true;
            case 11: await ManageCategories(user); return true;
            case 12: await ManageClients(user); return true;
            case 13: await ManageOrdersStatus(user); return true;
            case 14: await ManageSuppliers(user); return true;
            case 15: await ManageDeliveries(user); return true;
            case 16: await ShowAllOrders(user); return true; // Added option
            default:
                ConsoleHelper.WriteWarning("Nieznana opcja dla Twojej roli.");
                return true;
        }
    }

     private async Task<bool> HandleAdminChoice(int choice, User user)
    {
        // Try handling as Herbalist first for shared options
        // Adjust range if Herbalist options change
        if ((choice >= 1 && choice <= 3) || (choice >= 10 && choice <= 16))
        {
             return await HandleHerbalistChoice(choice, user);
        }
         if (choice == 0) return false; // Logout

        switch (choice)
        {
            // Admin specific options
            case 20: await ManageBillTypes(user); return true;
            case 21: await ManageBills(user); return true;
            case 22: await GenerateReports(user); return true;
            default:
                ConsoleHelper.WriteWarning("Nieznana opcja dla Twojej roli.");
                return true;
        }
    }


    // --- Action Methods Implementation (with logging and error handling) ---

    private async Task ShowAllProducts(User user)
    {
        await _logger.LogInfoAsync($"User '{user.Login}' executing: ShowAllProducts");
        Console.Clear();
        ConsoleHelper.DisplayHeader("Lista Produktów");
        try
        {
            var products = await _dbService.GetAllProductsAsync();
            if (!products.Any())
            {
                Console.WriteLine("Brak produktów w bazie.");
                return;
            }
            // Improved formatting
            Console.WriteLine($"{"ID",-5} | {"Nazwa",-35} | {"Cena",-12} | {"Kat.ID",-7} | {"Opis"}");
            Console.WriteLine(new string('-', Console.WindowWidth > 80 ? 80 : Console.WindowWidth -1)); // Adjust line width
            foreach (var p in products)
            {
                string description = p.Description ?? "Brak";
                // Truncate description if too long for console display
                if (description.Length > 30) description = description.Substring(0, 27) + "...";
                Console.WriteLine($"{p.Id,-5} | {p.Name,-35} | {p.Price,-12:C} | {p.CategoryId?.ToString() ?? "Brak",-7} | {description}");
            }
        }
        catch (Exception ex)
        {
             // Error already logged by DB service if DB related
             // Log context here if needed, otherwise just inform user
             ConsoleHelper.WriteError($"Nie można pobrać listy produktów. {ex.Message}");
             // No need to log again unless adding more context
        }
    }

    private async Task PlaceNewOrder(User user)
    {
        await _logger.LogInfoAsync($"User '{user.Login}' executing: PlaceNewOrder");
        Console.Clear();
        ConsoleHelper.DisplayHeader("Składanie nowego zamówienia");

        try
        {
            // 1. Get Client ID based on logged-in user
            // Assuming user login is their email for simplicity, adjust if needed
            var client = await _dbService.GetClientByEmailAsync(user.Login);
            if (client == null)
            {
                ConsoleHelper.WriteError("Nie znaleziono profilu klienta dla zalogowanego użytkownika. Skontaktuj się z administratorem.");
                await _logger.LogWarningAsync($"Could not find client profile for user '{user.Login}' (email) to place order.");
                return;
            }

            // 2. Show available products
            var products = await _dbService.GetAllProductsAsync();
            if (!products.Any())
            {
                Console.WriteLine("Obecnie brak dostępnych produktów do zamówienia.");
                return;
            }
            Console.WriteLine("Dostępne produkty:");
            Console.WriteLine($"{"ID",-5} | {"Nazwa",-35} | {"Cena",-12}");
            Console.WriteLine(new string('-', 60));
             foreach (var p in products)
            {
                Console.WriteLine($"{p.Id,-5} | {p.Name,-35} | {p.Price,-12:C}");
            }
            Console.WriteLine(new string('-', 60));


            // 3. Collect order details (products and quantities)
            var orderDetails = new List<OrderDetail>();
            bool addMore = true;
            while (addMore)
            {
                int productId = ConsoleHelper.ReadInt("Podaj ID produktu (lub 0 aby zakończyć):", 0);
                if (productId == 0)
                {
                    addMore = false;
                    continue;
                }

                var selectedProduct = products.FirstOrDefault(p => p.Id == productId);
                if (selectedProduct == null)
                {
                    ConsoleHelper.WriteWarning("Nie znaleziono produktu o podanym ID.");
                    continue;
                }

                int quantity = ConsoleHelper.ReadInt($"Podaj ilość dla '{selectedProduct.Name}':", 1); // Must order at least 1

                // Check if product already in cart, update quantity or add new
                var existingDetail = orderDetails.FirstOrDefault(d => d.ProductId == productId);
                 if (existingDetail != null)
                 {
                     existingDetail.Quantity += quantity;
                      Console.WriteLine($"Zaktualizowano ilość dla '{selectedProduct.Name}' do {existingDetail.Quantity}.");
                 }
                 else
                 {
                    orderDetails.Add(new OrderDetail { ProductId = productId, Quantity = quantity });
                     Console.WriteLine($"Dodano '{selectedProduct.Name}' (ilość: {quantity}) do zamówienia.");
                 }

                // Ask to add more? Simplified: loop continues until 0 is entered.
            }

            if (!orderDetails.Any())
            {
                Console.WriteLine("Anulowano składanie zamówienia (brak produktów).");
                return;
            }

            // 4. Display summary and confirm
             Console.WriteLine("\n--- Podsumowanie zamówienia ---");
             decimal totalAmount = 0;
             foreach(var detail in orderDetails)
             {
                 var product = products.First(p => p.Id == detail.ProductId); // Safe because we checked earlier
                 Console.WriteLine($"- {product.Name} x {detail.Quantity} = {product.Price * detail.Quantity:C}");
                 totalAmount += product.Price * detail.Quantity;
             }
             Console.WriteLine($"------------------------------\nSuma: {totalAmount:C}");

             if (!ConsoleHelper.Confirm("Czy chcesz złożyć to zamówienie?"))
             {
                 Console.WriteLine("Anulowano składanie zamówienia.");
                 await _logger.LogInfoAsync($"User '{user.Login}' cancelled order placement.");
                 return;
             }


            // 5. Create Order and OrderDetail records using transaction
            var newOrder = new Order
            {
                ClientId = client.Id,
                OrderDate = DateTime.Now, // Use local time or UTC based on preference/DB settings
                Status = OrderStatus.Nowe // Initial status
            };

            // Use the transactional method
            int newOrderId = await _dbService.CreateOrderWithDetailsAsync(newOrder, orderDetails);

            ConsoleHelper.WriteSuccess($"Zamówienie nr {newOrderId} zostało pomyślnie złożone!");
            await _logger.LogInfoAsync($"User '{user.Login}' successfully placed order ID {newOrderId} for client ID {client.Id}. Total amount: {totalAmount:C}");

        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Wystąpił błąd podczas składania zamówienia: {ex.Message}");
            await _logger.LogErrorAsync($"Error during PlaceNewOrder for user '{user.Login}'.", ex);
        }
    }

     private async Task ShowMyOrders(User user)
    {
        await _logger.LogInfoAsync($"User '{user.Login}' executing: ShowMyOrders");
         Console.Clear();
        ConsoleHelper.DisplayHeader("Moje Zamówienia");

        try
        {
            // Get Client ID
             var client = await _dbService.GetClientByEmailAsync(user.Login);
            if (client == null)
            {
                ConsoleHelper.WriteError("Nie znaleziono profilu klienta.");
                return;
            }

            // Fetch orders for this client
            var orders = await _dbService.GetOrdersByClientIdAsync(client.Id);
            if (!orders.Any())
            {
                 Console.WriteLine("Nie masz jeszcze żadnych zamówień.");
                 return;
            }

             Console.WriteLine($"{"ID Zam.",-10} | {"Data",-20} | {"Status",-15} | Szczegóły?");
             Console.WriteLine(new string('-', 60));
             foreach (var order in orders)
            {
                 Console.WriteLine($"{order.Id,-10} | {order.OrderDate,-20:yyyy-MM-dd HH:mm} | {order.Status,-15} | (Wpisz ID by zobaczyć)");
            }
             Console.WriteLine(new string('-', 60));

             // Optionally allow viewing details
             int orderIdToShow = ConsoleHelper.ReadInt("Podaj ID zamówienia, aby zobaczyć szczegóły (lub 0 aby wrócić):", 0);

             if (orderIdToShow > 0 && orders.Any(o => o.Id == orderIdToShow))
             {
                 await ShowOrderDetails(orderIdToShow);
             }
        }
        catch (Exception ex)
        {
             ConsoleHelper.WriteError($"Nie można pobrać zamówień: {ex.Message}");
            // Logging handled by lower layers or catch block above
        }
    }

     // Helper to show details of a specific order
    private async Task ShowOrderDetails(int orderId)
    {
         Console.WriteLine($"\n--- Szczegóły zamówienia nr {orderId} ---");
         try
         {
             var details = await _dbService.GetOrderDetailsAsync(orderId);
             if (!details.Any())
             {
                 Console.WriteLine("Brak szczegółów dla tego zamówienia (lub błąd pobierania).");
                 return;
             }

             decimal totalAmount = 0;
              Console.WriteLine($"{"Produkt ID",-10} | {"Nazwa produktu",-30} | {"Ilość",-6} | {"Cena jednostkowa",-18} | {"Suma"}");
              Console.WriteLine(new string('-', 85));
              foreach (var detail in details)
             {
                 // Fetch product info - could be optimized by joining in GetOrderDetailsAsync
                 var product = await _dbService.GetProductByIdAsync(detail.ProductId);
                 string productName = product?.Name ?? "Nieznany Produkt";
                 decimal price = product?.Price ?? 0;
                 decimal lineTotal = price * detail.Quantity;
                 totalAmount += lineTotal;

                  Console.WriteLine($"{detail.ProductId,-10} | {productName,-30} | {detail.Quantity,-6} | {price,-18:C} | {lineTotal:C}");
             }
             Console.WriteLine(new string('-', 85));
              Console.WriteLine($"SUMA CAŁKOWITA: {totalAmount:C}");

         }
          catch (Exception ex)
         {
              ConsoleHelper.WriteError($"Nie można pobrać szczegółów zamówienia: {ex.Message}");
             // Log if necessary, though DB service likely caught DB exceptions
         }
    }


    private async Task ManageProducts(User user)
    {
        await _logger.LogInfoAsync($"User '{user.Login}' executing: ManageProducts");
        bool stayInMenu = true;
        while(stayInMenu)
        {
             Console.Clear();
            ConsoleHelper.DisplayHeader("Zarządzanie Produktami");
            Console.WriteLine("1. Wyświetl wszystkie produkty");
            Console.WriteLine("2. Dodaj nowy produkt");
            Console.WriteLine("3. Edytuj produkt");
            Console.WriteLine("4. Usuń produkt");
            Console.WriteLine("0. Wróć do menu głównego");
             Console.WriteLine("---------------------------");
            int choice = ConsoleHelper.ReadInt("Wybierz opcję:", 0, 4);

            switch(choice)
            {
                case 1: await ShowAllProducts(user); break; // Reuse existing method
                case 2: await AddNewProduct(user); break;
                case 3: await EditProduct(user); break;
                case 4: await DeleteProduct(user); break;
                case 0: stayInMenu = false; break;
            }
             if (stayInMenu) { Console.WriteLine("\nNaciśnij klawisz..."); Console.ReadKey(); }
        }
    }

     // --- Sub-methods for ManageProducts ---
     private async Task AddNewProduct(User user)
     {
          await _logger.LogInfoAsync($"User '{user.Login}' attempting to add new product.");
         ConsoleHelper.DisplayHeader("Dodawanie Nowego Produktu");
         try
         {
             string name = ConsoleHelper.ReadString("Nazwa produktu:");
             string? description = ConsoleHelper.ReadString("Opis (opcjonalnie):", allowEmpty: true);
             decimal price = ConsoleHelper.ReadDecimal("Cena (np. 10.99):", 0.01m); // Minimum price > 0
             int? categoryId = ConsoleHelper.ReadInt("ID Kategorii (lub 0 jeśli brak):", 0); // TODO: Show categories first?

             var newProduct = new Product
             {
                 Name = name,
                 Description = string.IsNullOrWhiteSpace(description) ? null : description,
                 Price = price,
                 CategoryId = (categoryId == 0) ? null : categoryId
             };

             // TODO: Validate if categoryId exists if provided > 0

             await _dbService.AddProductAsync(newProduct);
             ConsoleHelper.WriteSuccess($"Produkt '{name}' został dodany.");
             await _logger.LogInfoAsync($"User '{user.Login}' added product '{name}'.");
         }
         catch(Exception ex)
         {
              ConsoleHelper.WriteError($"Nie można dodać produktu: {ex.Message}");
             await _logger.LogErrorAsync($"Error adding product by user '{user.Login}'.", ex);
         }
     }

     private async Task EditProduct(User user)
    {
         await _logger.LogInfoAsync($"User '{user.Login}' attempting to edit product.");
         ConsoleHelper.DisplayHeader("Edycja Produktu");
         int productId = ConsoleHelper.ReadInt("Podaj ID produktu do edycji:");
         try
         {
             var product = await _dbService.GetProductByIdAsync(productId);
             if (product == null)
             {
                 ConsoleHelper.WriteWarning($"Nie znaleziono produktu o ID: {productId}");
                 return;
             }

              Console.WriteLine($"Edytujesz: {product.Name} (Cena: {product.Price:C}, Kategoria: {product.CategoryId?.ToString() ?? "Brak"})");
             string name = ConsoleHelper.ReadString($"Nowa nazwa (lub Enter by zostawić '{product.Name}'):", allowEmpty: true);
             string? description = ConsoleHelper.ReadString($"Nowy opis (lub Enter by zostawić):", allowEmpty: true); // Handle null/empty description from DB
             string priceStr = ConsoleHelper.ReadString($"Nowa cena (lub Enter by zostawić '{product.Price:C}'):", allowEmpty: true);
             string categoryStr = ConsoleHelper.ReadString($"Nowe ID Kategorii (lub Enter by zostawić '{product.CategoryId?.ToString() ?? "Brak"}'):", allowEmpty: true);

             // Update only if new value provided
             if (!string.IsNullOrWhiteSpace(name)) product.Name = name;
             // If description was null and user enters nothing, keep it null. If it had value, allow empty string or new value.
             if (description != null) product.Description = string.IsNullOrWhiteSpace(description) ? null : description;

             if (decimal.TryParse(priceStr?.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal newPrice) && newPrice > 0)
             {
                product.Price = newPrice;
             }
             if (int.TryParse(categoryStr, out int newCatId))
             {
                 // TODO: Validate category ID exists if newCatId > 0
                 product.CategoryId = newCatId == 0 ? null : newCatId;
             }

             await _dbService.UpdateProductAsync(product);
             ConsoleHelper.WriteSuccess($"Produkt ID {productId} został zaktualizowany.");
             await _logger.LogInfoAsync($"User '{user.Login}' updated product ID {productId}.");
         }
         catch (Exception ex)
         {
             ConsoleHelper.WriteError($"Nie można edytować produktu: {ex.Message}");
             await _logger.LogErrorAsync($"Error editing product ID {productId} by user '{user.Login}'.", ex);
         }
    }

     private async Task DeleteProduct(User user)
    {
         await _logger.LogInfoAsync($"User '{user.Login}' attempting to delete product.");
         ConsoleHelper.DisplayHeader("Usuwanie Produktu");
         int productId = ConsoleHelper.ReadInt("Podaj ID produktu do usunięcia:");
         try
         {
              var product = await _dbService.GetProductByIdAsync(productId); // Check if exists first
              if (product == null)
             {
                 ConsoleHelper.WriteWarning($"Nie znaleziono produktu o ID: {productId}");
                 return;
             }

              if (ConsoleHelper.Confirm($"Czy na pewno chcesz usunąć produkt '{product.Name}' (ID: {productId})? Może to wpłynąć na zamówienia!"))
             {
                 await _dbService.DeleteProductAsync(productId);
                 ConsoleHelper.WriteSuccess($"Produkt ID {productId} został usunięty.");
                 await _logger.LogInfoAsync($"User '{user.Login}' deleted product ID {productId} ('{product.Name}').");
             }
             else
             {
                  Console.WriteLine("Anulowano usuwanie.");
                 await _logger.LogInfoAsync($"User '{user.Login}' cancelled deletion of product ID {productId}.");
             }
         }
         catch (Exception ex) // Catch potential FK constraint errors if schema differs
         {
             ConsoleHelper.WriteError($"Nie można usunąć produktu: {ex.Message}");
              await _logger.LogErrorAsync($"Error deleting product ID {productId} by user '{user.Login}'.", ex);
         }
    }


    // --- TODO: Implement other ManageX methods ---
    private async Task ManageCategories(User user)
    {
        await _logger.LogInfoAsync($"User '{user.Login}' executing: ManageCategories");
        ConsoleHelper.DisplayHeader("Zarządzanie Kategoriami");
        Console.WriteLine("[Funkcjonalność do implementacji]");
        // List, Add, Edit, Delete Categories
        await Task.Delay(100); // Placeholder
    }
    private async Task ManageClients(User user)
    {
        await _logger.LogInfoAsync($"User '{user.Login}' executing: ManageClients");
        ConsoleHelper.DisplayHeader("Zarządzanie Klientami");
        Console.WriteLine("[Funkcjonalność do implementacji]");
        // List, Add (with Address?), Edit, View Orders
        await Task.Delay(100); // Placeholder
    }
     private async Task ManageSuppliers(User user)
    {
        await _logger.LogInfoAsync($"User '{user.Login}' executing: ManageSuppliers");
        ConsoleHelper.DisplayHeader("Zarządzanie Dostawcami");
        Console.WriteLine("[Funkcjonalność do implementacji]");
        // List, Add (with Address?), Edit
        await Task.Delay(100); // Placeholder
    }
     private async Task ManageDeliveries(User user)
    {
        await _logger.LogInfoAsync($"User '{user.Login}' executing: ManageDeliveries");
        ConsoleHelper.DisplayHeader("Zarządzanie Dostawami");
        Console.WriteLine("[Funkcjonalność do implementacji]");
        // List deliveries, Add delivery record (linking supplier, product, quantity, date?) - Needs DB table adjustments?
        await Task.Delay(100); // Placeholder
    }
     private async Task ManageBills(User user)
    {
        await _logger.LogInfoAsync($"User '{user.Login}' executing: ManageBills");
        ConsoleHelper.DisplayHeader("Zarządzanie Rachunkami");
        Console.WriteLine("[Funkcjonalność do implementacji]");
         // List, Add (requires Bill Type), View
        await Task.Delay(100); // Placeholder
    }

      private async Task ManageBillTypes(User user)
    {
        await _logger.LogInfoAsync($"User '{user.Login}' executing: ManageBillTypes");
        ConsoleHelper.DisplayHeader("Zarządzanie Typami Rachunków");
        Console.WriteLine("[Funkcjonalność do implementacji]");
         // List, Add, Edit, Delete
        await Task.Delay(100); // Placeholder
    }

     private async Task GenerateReports(User user)
    {
        await _logger.LogInfoAsync($"User '{user.Login}' executing: GenerateReports");
        ConsoleHelper.DisplayHeader("Generowanie Raportów");
        Console.WriteLine("[Funkcjonalność NIEDOSTĘPNA]");
        await Task.Delay(100); // Placeholder
    }

     // --- Manage Orders Status & View All ---
    private async Task ManageOrdersStatus(User user)
    {
         await _logger.LogInfoAsync($"User '{user.Login}' executing: ManageOrdersStatus");
         Console.Clear();
         ConsoleHelper.DisplayHeader("Zarządzanie Statusami Zamówień");
         try
         {
             // Simplified: Show all 'New' or 'In Progress' orders
             // TODO: Fetch only relevant orders
             var allOrders = new List<Order>(); // Replace with actual DB call
             Console.WriteLine("Pobieranie zamówień..."); // Placeholder
             // var ordersToManage = await _dbService.GetOrdersByStatusAsync(OrderStatus.Nowe, OrderStatus.W_trakcie);

             if (!allOrders.Any())
             {
                  Console.WriteLine("Brak zamówień wymagających zmiany statusu.");
                  return;
             }

              Console.WriteLine($"{"ID Zam.",-10} | {"Klient ID",-10} | {"Data",-20} | {"Aktualny Status",-15}");
              Console.WriteLine(new string('-', 65));
              foreach(var order in allOrders)
              {
                  Console.WriteLine($"{order.Id,-10} | {order.ClientId?.ToString() ?? "Brak",-10} | {order.OrderDate,-20:yyyy-MM-dd HH:mm} | {order.Status,-15}");
              }
               Console.WriteLine(new string('-', 65));

              int orderId = ConsoleHelper.ReadInt("Podaj ID zamówienia do zmiany statusu (lub 0):", 0);
              if (orderId == 0 || !allOrders.Any(o => o.Id == orderId)) return;

              var orderToUpdate = allOrders.First(o => o.Id == orderId);

              Console.WriteLine("Wybierz nowy status:");
              var statuses = Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().ToList();
              for(int i = 0; i < statuses.Count; i++)
              {
                  Console.WriteLine($"{i + 1}. {statuses[i]}");
              }
              int statusChoice = ConsoleHelper.ReadInt("Numer nowego statusu:", 1, statuses.Count);
              OrderStatus newStatus = statuses[statusChoice - 1];

              if (newStatus == orderToUpdate.Status)
              {
                   Console.WriteLine("Status nie został zmieniony.");
                   return;
              }

              if (ConsoleHelper.Confirm($"Zmieniasz status zamówienia {orderId} na '{newStatus}'. Kontynuować?"))
              {
                  await _dbService.UpdateOrderStatusAsync(orderId, newStatus);
                   ConsoleHelper.WriteSuccess($"Status zamówienia {orderId} został zmieniony na {newStatus}.");
                  await _logger.LogInfoAsync($"User '{user.Login}' updated order ID {orderId} status from {orderToUpdate.Status} to {newStatus}.");
              }
              else
              {
                   Console.WriteLine("Anulowano zmianę statusu.");
              }
         }
          catch (Exception ex)
         {
              ConsoleHelper.WriteError($"Błąd podczas zarządzania statusami zamówień: {ex.Message}");
             // Log if needed
         }
    }

     private async Task ShowAllOrders(User user) // Added for Herbalist/Admin
    {
         await _logger.LogInfoAsync($"User '{user.Login}' executing: ShowAllOrders");
         Console.Clear();
         ConsoleHelper.DisplayHeader("Wszystkie Zamówienia");
         try
         {
              // TODO: Implement _dbService.GetAllOrdersAsync()
              var allOrders = new List<Order>(); // Replace with actual DB call
               Console.WriteLine("Pobieranie wszystkich zamówień..."); // Placeholder

              if (!allOrders.Any())
             {
                  Console.WriteLine("Brak jakichkolwiek zamówień w systemie.");
                  return;
             }

               Console.WriteLine($"{"ID Zam.",-10} | {"Klient ID",-10} | {"Data",-20} | {"Status",-15}");
               Console.WriteLine(new string('-', 65));
               foreach (var order in allOrders)
              {
                   Console.WriteLine($"{order.Id,-10} | {order.ClientId?.ToString() ?? "Brak",-10} | {order.OrderDate,-20:yyyy-MM-dd HH:mm} | {order.Status,-15}");
              }
                Console.WriteLine(new string('-', 65));

               // Optionally allow viewing details
               int orderIdToShow = ConsoleHelper.ReadInt("Podaj ID zamówienia, aby zobaczyć szczegóły (lub 0 aby wrócić):", 0);

                if (orderIdToShow > 0 && allOrders.Any(o => o.Id == orderIdToShow))
               {
                   await ShowOrderDetails(orderIdToShow); // Reuse helper
               }
         }
          catch (Exception ex)
         {
              ConsoleHelper.WriteError($"Błąd podczas pobierania wszystkich zamówień: {ex.Message}");
             // Log if needed
         }
    }
}