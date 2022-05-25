﻿using AutoBarBar.Models;
using AutoBarBar.Services;
using AutoBarBar.Views;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Forms;
using Newtonsoft.Json;
using static AutoBarBar.Constants;
using static AutoBarBar.DateTimeHelper;
using ZXing;

namespace AutoBarBar.ViewModels
{
    public class BartenderHomePageViewModel : BaseViewModel, IQueryAttributable
    {
        readonly IProductService productService;
        readonly IActiveTabService activeTabService;
        readonly IOrderLineService orderLineService;
        readonly IToastService toastService;

        public AsyncCommand GetReloadBalanceAmountCommand { get; }
        public AsyncCommand ShowScanCommand { get; }
        public AsyncCommand EndTransactionCommand { get; }
        public AsyncCommand AddOrderLineCommand { get; }
        public AsyncCommand<string> SearchProductCommand { get; }
        public AsyncCommand<string> SearchCustomerCommand { get; }

        public Command SwitchUserCommand { get; }
        public Command AddProductToOrderLineCommand { get; }
        public Command IncreaseQuantityCommand { get; }
        public Command DecreaseQuantityCommand { get; }

        public ICommand Test { get; }

        Reward DummyReward = new Reward()
        {
            Id = "dummy",
            Name = "-- None --"
        };
        public string[] times = { "7:30PM", "8:30PM", "10:30PM" };

        public BartenderHomePageViewModel()
        {
            productService = DependencyService.Get<IProductService>();
            activeTabService = DependencyService.Get<IActiveTabService>();
            orderLineService = DependencyService.Get<IOrderLineService>();
            toastService = DependencyService.Get<IToastService>();

            Title = "Bartender Home Page";
            Customers = new ObservableRangeCollection<Customer>();
            Users = new ObservableRangeCollection<User>();
            Products = new ObservableRangeCollection<Product>();
            OrderLines = new ObservableRangeCollection<OrderLine>();
            Orders = new ObservableRangeCollection<Order>();
            Rewards = new ObservableRangeCollection<Reward>();
            ActiveTabs = new ObservableRangeCollection<ActiveTab>();

            NewOrderLines = new ObservableCollection<OrderLine>();
            
            PopulateData();

            ShowScanCommand = new AsyncCommand(ShowScan);
            GetReloadBalanceAmountCommand = new AsyncCommand(GetReloadBalanceAmount);
            EndTransactionCommand = new AsyncCommand(EndTransaction);
            AddOrderLineCommand = new AsyncCommand(AddOrderLine);
            SearchCustomerCommand = new AsyncCommand<string>(SearchCustomer);
            SearchProductCommand = new AsyncCommand<string>(SearchProduct);
            SwitchUserCommand = new Command<object>(SwitchUser);
            AddProductToOrderLineCommand = new Command<Product>(AddProductToOrderLine);
            IncreaseQuantityCommand = new Command<OrderLine>(IncreaseQuantity);
            DecreaseQuantityCommand = new Command<OrderLine>(DecreaseQuantity);

            SelectedProduct = null;
            TotalOrderLinesCost = 0;
            CanAddNewOrderLine = false;
            SelectedReward = DummyReward;

            Test = new Command(TestMe);
        }

        void TestMe()
        {
            var a = SelectedUser;
        }

        void PopulateData()
        {
            try
            {
                //List<int> orderIDs = new List<int>();
                var getRewardsTask = GetItemsAsync(Rewards, RewardDataStore);
                var productsTask = productService.GetProducts();
                var activeTabsTask = activeTabService.GetActiveTabs();
                string orderIDs = customerIDs = string.Empty;

                Task[] tasks = new Task[]
                {
                getRewardsTask, productsTask, activeTabsTask
                };
                Task.WaitAll(tasks);

                ActiveTabs.AddRange(activeTabsTask.Result);
                Products.AddRange(productsTask.Result);

                foreach (var at in ActiveTabs)
                {
                    Customers.Add(at.ATCustomer);
                    Orders.Add(at.ATOrder);
                    Users.Add(at.ATUser);
                    orderIDs += $", {at.ATOrder.ID}";
                    customerIDs += $", {at.ATCustomer.ID}"; 
                }

                if(!string.IsNullOrEmpty(orderIDs))
                {
                    orderIDs.Remove(0, 2);
                }
                if (!string.IsNullOrEmpty(customerIDs))
                {
                    customerIDs.Remove(0, 2);
                }

                var orderLinesTask = orderLineService.GetOrderLines(orderIDs);
                orderLinesTask.Wait();
                foreach(OrderLine ol in orderLinesTask.Result)
                {
                    ol.ProductName = Products.FirstOrDefault(p => p.ID == ol.ProductID).Name;
                    ol.SubTotal = ol.Quantity * ol.UnitPrice;
                    OrderLines.Add(ol);
                }
            }
            catch(Exception e)
            {
                var a = e.Message;
            }
        }

        async Task GetItemsAsync<TModel>(ObservableRangeCollection<TModel> list, IDataStore<TModel> dataStore)
        {
            var listFromDb = await dataStore.GetItemsAsync();
            list.AddRange(listFromDb);
        }

        async Task ShowScan()
        {
            await Shell.Current.GoToAsync($"//{nameof(BartenderHomePage)}/{nameof(ScanPage)}?ids={customerIDs}");
        }
        
        async Task GetReloadBalanceAmount()
        {
            string ans = await Application.Current.MainPage.DisplayPromptAsync("Balance", "Enter amount:", "Add", "Cancel", null, -1, Keyboard.Numeric, "");
            if(decimal.TryParse(ans, out decimal num))
            {
                foreach(var c in Customers)
                {
                    if(c.ID == SelectedCustomer.ID)
                    {
                        c.Balance += num; 
                        await activeTabService.AddBalance(c.ID, c.Balance, GetPHTime());
                        await Application.Current.MainPage.DisplayAlert("Success", "Balance has been added.", "Ok");
                        CurrentBalance = c.Balance;
                    }
                }
            }
        }

        async Task EndTransaction()
        {
            await App.Current.MainPage.DisplayAlert("Success", "Customer transaction has ended.", "Ok");

            CurrentOrder.OrderStatus = 2;
            Orders.Add(CurrentOrder);
            Customers.Remove(SelectedCustomer);
        }

        async Task AddOrderLine()
        {
            await App.Current.MainPage.DisplayAlert("Success", "You have successfully ordered.", "Thanks");
            string time = "9:45PM";
            int pe;
            var group = from ol in NewOrderLines
                        group ol by time into newOl
                        select newOl;
            CurrentOrderLineGroup.Add(group.ElementAt(0));
            OrderLines.AddRange(NewOrderLines);
            NewOrderLines.Clear();

            CurrentBalance = SelectedCustomer.Balance -= TotalOrderLinesCost;
            TotalOrderPrice = CurrentOrder.TotalPrice += Convert.ToDouble(TotalOrderLinesCost);
            PointsEarned = CurrentOrder.PointsEarned += (pe = (int)CurrentOrder.TotalPrice / 1000) != 0 ? pe * 100 : 0;
            TotalOrderLinesCost = 0;
        }

        async Task SearchProduct(string arg)
        {
            await SearchItemsAsync<Product>(Products, ProductDataStore, arg.ToLowerInvariant());
        }

        async Task SearchCustomer(string arg)
        {
            await SearchItemsAsync<Customer>(Customers, CustomerDataStore, arg.ToLowerInvariant());
        }

        async Task SearchItemsAsync<TModel>(ObservableRangeCollection<TModel> List, IDataStore<TModel> dataStore, string arg)
        {
            var list = await dataStore.GetSearchResults(arg);
            List.ReplaceRange(list);
        }

        void SwitchUser(object o)
        {
            if (o == null)
                return;

            User user = o as User;
            
            SelectedUser = user;
            SelectedCustomer = Customers.FirstOrDefault(c => c.UserID == user.ID);
            // Extend BaseModel
            CurrentBalance = SelectedCustomer.Balance;
            CurrentOrder = Orders.First(order => order.CustomerID == SelectedCustomer.ID);
            CurrentOrderLines = new ObservableCollection<OrderLine>(OrderLines.Where(ol => ol.OrderID == CurrentOrder.ID));
            PointsEarned = CurrentOrder.PointsEarned;
            TotalOrderPrice = CurrentOrder.TotalPrice;

            var group = from ol in CurrentOrderLines
                        group ol by ol.CreatedOn into newGroup
                        orderby newGroup.Key descending
                        select newGroup;
            CurrentOrderLineGroup = new ObservableCollection<IGrouping<string, OrderLine>>(group);
            NewOrderLines.Clear();
            foreach (var colg in CurrentOrderLineGroup)
            {
                decimal total = 0;
                foreach (var ol in colg)
                {
                    total += ol.SubTotal;
                }
            }
        }

        void AddProductToOrderLine (Product p)
        {
            SelectedProduct = null;

            if (CanAddNewOrderLine == false)
                CanAddNewOrderLine = true;

            var newTotalCost = p.UnitPrice + TotalOrderLinesCost;
            if(newTotalCost > CurrentBalance)
            {
                toastService.ShowLongMessage("Insufficient balance.");
                return;
            }

            int x;
            for(x = 0; x < NewOrderLines.Count && NewOrderLines[x].ProductID != p.ID; x++) { }
            if(x == NewOrderLines.Count)
            {
                NewOrderLines.Add(new OrderLine
                {
                    TempID = Guid.NewGuid().ToString(),
                    OrderID = CurrentOrder.ID,
                    ProductID = p.ID,
                    UnitPrice = p.UnitPrice,
                    Quantity = 1,
                    CreatedBy = StaffUser.ID,

                    CustomerName = SelectedUser.FirstName,
                    ProductName = p.Name,
                    ProductImgUrl = p.ImageLink,
                    SubTotal = p.UnitPrice
                }) ;
                TotalOrderLinesCost = newTotalCost;
            } 
            else
            {
                toastService.ShowShortMessage($"{p.Name} is already in the order.");
            }
        }

        void IncreaseQuantity(OrderLine Ol)
        {
            var newTotal = TotalOrderLinesCost + Ol.UnitPrice;
            if (newTotal > SelectedCustomer.Balance)
            {
                toastService.ShowLongMessage("Insufficient balance.");
                return;
            }

            foreach(var nol in NewOrderLines)
            {
                if (nol.TempID == Ol.TempID)
                {
                    nol.Quantity++;
                    nol.SubTotal += Ol.UnitPrice;
                    TotalOrderLinesCost = newTotal;
                    break;
                }
            }
        }

        void DecreaseQuantity(OrderLine Ol)
        {
            foreach(var nol in NewOrderLines)
            {
                if(nol.TempID == Ol.TempID)
                {
                    if(--nol.Quantity == 0)
                    {   
                        NewOrderLines.Remove(nol);
                        if (NewOrderLines.Count == 0 && CanAddNewOrderLine == true)
                            CanAddNewOrderLine = false;
                    } else
                    {
                        nol.SubTotal -= nol.UnitPrice;
                    }

                    TotalOrderLinesCost -= Ol.UnitPrice;
                    break;
                }
            }
        }

        public void ApplyQueryAttributes(IDictionary<string, string> query)
        {
            if(query.Count == 0)
            {
                return;
            }

            if (query.ContainsKey("user"))
            {
                string user = HttpUtility.UrlDecode(query["user"]);
                StaffUser = JsonConvert.DeserializeObject<User>(Uri.UnescapeDataString(user));
            }

            if(query.ContainsKey("newTab"))
            {
                string at = HttpUtility.UrlDecode(query["newTab"]);
                ActiveTab activeTab = JsonConvert.DeserializeObject<ActiveTab>(Uri.UnescapeDataString(at));
                customerIDs += $", {activeTab.ATCustomer.ID}";
            }
        }

        #region Getters setters
        #region Customers
        ObservableRangeCollection<Customer> customers;
        public ObservableRangeCollection<Customer> Customers
        {
            get { return customers; }
            set { SetProperty(ref customers, value); }
        }

        Customer selectedCustomer;
        public Customer SelectedCustomer
        {
            get { return selectedCustomer; }
            set { SetProperty(ref selectedCustomer, value); }
        }

        string customerIDs;
        public string CustomerIDs
        {
            get => customerIDs;
            set => SetProperty(ref customerIDs, value);
        }

        decimal currentBalance;
        public decimal CurrentBalance
        {
            get => currentBalance;
            set => SetProperty(ref currentBalance, value);
        }
        #endregion

        #region Users
        ObservableRangeCollection<User> users;
        public ObservableRangeCollection<User> Users
        {
            get => users;
            set => SetProperty(ref users, value);
        }

        User selectedUser;
        public User SelectedUser
        {
            get { return selectedUser; }
            set { SetProperty(ref selectedUser, value); }
        }

        User staffUser;
        public User StaffUser { 
            get => staffUser;
            set => SetProperty(ref staffUser, value);   
        }
        #endregion

        #region Products
        ObservableRangeCollection<Product> products;
        public ObservableRangeCollection<Product> Products
        {
            get { return products; }
            set { SetProperty(ref products, value); }
        }

        Product selectedProduct;
        public Product SelectedProduct
        {
            get => selectedProduct;
            set => SetProperty(ref selectedProduct, value);
        }
        #endregion

        #region Orders
        ObservableRangeCollection<Order> orders;
        public ObservableRangeCollection<Order> Orders
        {
            get { return orders; }
            set { SetProperty(ref orders, value); }
        }

        Order currentOrder;
        public Order CurrentOrder
        {
            get { return currentOrder; }
            set { SetProperty(ref currentOrder, value); }
        }

        double totalOrderPrice;
        public double TotalOrderPrice
        {
            get => totalOrderPrice;
            set => SetProperty(ref totalOrderPrice, value);
        }

        decimal pointsEarned;
        public decimal PointsEarned
        {
            get => pointsEarned;
            set => SetProperty(ref pointsEarned, value);
        }
        #endregion

        #region OrderLines
        ObservableRangeCollection<OrderLine> orderLines;
        public ObservableRangeCollection<OrderLine> OrderLines
        {
            get { return orderLines; }
            set { SetProperty(ref orderLines, value); }
        }

        ObservableCollection<OrderLine> currentOrderLines;
        public ObservableCollection<OrderLine> CurrentOrderLines
        {
            get { return currentOrderLines; }
            set { SetProperty(ref currentOrderLines, value); }
        }

        ObservableCollection<OrderLine> newOrderLines;
        public ObservableCollection<OrderLine> NewOrderLines
        {
            get => newOrderLines;
            set => SetProperty(ref newOrderLines, value);
        }

        ObservableCollection<IGrouping<string, OrderLine>> currentOrderLineGroup;
        public ObservableCollection<IGrouping<string, OrderLine>> CurrentOrderLineGroup
        {
            get { return currentOrderLineGroup; }
            set { SetProperty(ref currentOrderLineGroup, value); }
        }

        bool canAddNewOrderLine;
        public bool CanAddNewOrderLine
        {
            get => canAddNewOrderLine;
            set => SetProperty(ref canAddNewOrderLine, value);
        }

        decimal totalOrderLinesCost;
        public decimal TotalOrderLinesCost
        {
            get => totalOrderLinesCost;
            set => SetProperty(ref totalOrderLinesCost, value);
        }
        #endregion

        #region Rewards
        ObservableRangeCollection<Reward> rewards;
        public ObservableRangeCollection<Reward> Rewards
        {
            get { return rewards; }
            set { SetProperty(ref rewards, value); }
        }

        Reward selectedReward;
        public Reward SelectedReward
        {
            get { return selectedReward; }
            set { SetProperty(ref selectedReward, value); }
        }
        #endregion

        #region ActiveTabs
        ObservableRangeCollection<ActiveTab> activeTabs;
        public ObservableRangeCollection<ActiveTab> ActiveTabs
        {
            get => activeTabs;
            set => SetProperty(ref activeTabs, value);
        }
        #endregion
        #endregion

        #region Singleton
        static BartenderHomePageViewModel instance;
        public static BartenderHomePageViewModel Instance
        {
            get 
            {
                if(instance == null)
                {
                    instance = new BartenderHomePageViewModel();
                }
                return instance;
            }
        }
        #endregion
    }
}
