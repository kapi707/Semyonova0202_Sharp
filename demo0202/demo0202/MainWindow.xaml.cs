using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;

namespace demo0202
{
    public partial class MainWindow : Window
    {
        SemyonovaDemo0202Entities db = new SemyonovaDemo0202Entities();
        private List<RequestViewModel> currentRequestViewModels;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadRequests();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}");
            }
        }

        private void LoadRequests()
        {
            try
            {
                // Используем Include для загрузки связанных данных
                var requests = db.PartnerRequests
                    .Include(p => p.Partners.PartnerTypes)
                    .ToList();

                currentRequestViewModels = new List<RequestViewModel>();

                foreach (var request in requests)
                {
                    var partner = request.Partners;
                    var totalAmount = CalculateTotalAmount(request.ID);

                    currentRequestViewModels.Add(new RequestViewModel
                    {
                        RequestID = request.ID,
                        PartnerTypeInfo = $"{partner.PartnerTypes.TypeName} | {partner.CompanyName}",
                        Address = partner.Address,
                        ContactInfo = partner.Phone,
                        RatingInfo = $"Рейтинг: {partner.Rating}",
                        TotalAmount = $"Сумма: {totalAmount:N2} ₽",
                        TotalAmountValue = totalAmount
                    });
                }

                // Сортируем по убыванию суммы
                currentRequestViewModels = currentRequestViewModels
                    .OrderByDescending(r => r.TotalAmountValue)
                    .ToList();

                RequestsContainer.ItemsSource = currentRequestViewModels;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}");
            }
        }

        private decimal CalculateTotalAmount(int requestId)
        {
            var items = db.RequestItems
                .Where(x => x.RequestID == requestId)
                .Include("Products")
                .ToList();

            decimal total = 0;
            foreach (var item in items)
            {
                total += (item.Quantity ?? 0) * (item.Products?.MinPrice ?? 0);
            }
            return total;
        }

        private void BtnNewRequest_Click(object sender, RoutedEventArgs e)
        {
            var newRequestWindow = new NewRequestWindow();
            newRequestWindow.Closed += (s, args) => LoadRequests();
            newRequestWindow.ShowDialog();
        }

        private void BtnProducts_Click(object sender, RoutedEventArgs e)
        {
            var productsWindow = new ProductsCatalogWindow();
            productsWindow.ShowDialog();
        }

        private void BtnCalculations_Click(object sender, RoutedEventArgs e)
        {
            var calculationsWindow = new CalculationsWindow();
            calculationsWindow.ShowDialog();
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag != null)
            {
                int requestId = (int)border.Tag;
                var editWindow = new EditRequestWindow(requestId);

                // Подписываемся на событие обновления
                editWindow.RequestUpdated += EditWindow_RequestUpdated;

                editWindow.Closed += (s, args) =>
                {
                    editWindow.RequestUpdated -= EditWindow_RequestUpdated;
                    LoadRequests(); // Полная перезагрузка при закрытии
                };

                editWindow.ShowDialog();
            }
        }

        // Обработчик события обновления
        private void EditWindow_RequestUpdated(int updatedRequestId)
        {
            // Обновляем данные через Dispatcher для работы из другого потока
            Dispatcher.Invoke(() =>
            {
                RefreshSingleRequest(updatedRequestId);
            });
        }

        // Метод для обновления одной заявки
        private void RefreshSingleRequest(int requestId)
        {
            try
            {
                // Очищаем контекст, чтобы получить свежие данные
                db.ChangeTracker.Entries().ToList().ForEach(e => e.Reload());

                var request = db.PartnerRequests
                    .Include(p => p.Partners.PartnerTypes)
                    .FirstOrDefault(r => r.ID == requestId);

                if (request != null)
                {
                    var partner = request.Partners;
                    var totalAmount = CalculateTotalAmount(requestId);

                    var existingViewModel = currentRequestViewModels
                        .FirstOrDefault(vm => vm.RequestID == requestId);

                    if (existingViewModel != null)
                    {
                        existingViewModel.PartnerTypeInfo = $"{partner.PartnerTypes.TypeName} | {partner.CompanyName}";
                        existingViewModel.Address = partner.Address;
                        existingViewModel.ContactInfo = partner.Phone;
                        existingViewModel.RatingInfo = $"Рейтинг: {partner.Rating}";
                        existingViewModel.TotalAmount = $"Сумма: {totalAmount:N2} ₽";
                        existingViewModel.TotalAmountValue = totalAmount;

                        // Сортируем заново
                        currentRequestViewModels = currentRequestViewModels
                            .OrderByDescending(r => r.TotalAmountValue)
                            .ToList();

                        // Обновляем ItemsSource
                        RequestsContainer.ItemsSource = null;
                        RequestsContainer.ItemsSource = currentRequestViewModels;
                    }
                    else
                    {
                        LoadRequests(); // Если не нашли - полная перезагрузка
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления заявки: {ex.Message}");
                LoadRequests(); // Полная перезагрузка при ошибке
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            db?.Dispose();
            base.OnClosed(e);
        }
    }

    public class RequestViewModel
    {
        public int RequestID { get; set; }
        public string PartnerTypeInfo { get; set; }
        public string Address { get; set; }
        public string ContactInfo { get; set; }
        public string RatingInfo { get; set; }
        public string TotalAmount { get; set; }
        public decimal TotalAmountValue { get; set; }
    }
}