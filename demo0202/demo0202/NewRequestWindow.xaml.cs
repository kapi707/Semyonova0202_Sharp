using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace demo0202
{

    public partial class NewRequestWindow : Window
    {
        // Подключение к базе данных
        SemyonovaDemo0202Entities db = new SemyonovaDemo0202Entities();

        // Временный список товаров заявки (пока не сохранены в БД)
        private List<RequestItemViewModel> requestItems;

        public NewRequestWindow()
        {
            InitializeComponent();
            // Инициализируем пустой список товаров
            requestItems = new List<RequestItemViewModel>();
            // Подписываемся на событие загрузки окна
            Loaded += NewRequestWindow_Loaded;
        }


        private void NewRequestWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadPartners();         // Загружаем список партнеров
                LoadProducts();         // Загружаем список товаров
                UpdateTotalAmount();    // Обновляем общую сумму (изначально 0)
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }


        private void LoadPartners()
        {
            CmbPartners.ItemsSource = db.Partners.ToList();
        }


        private void LoadProducts()
        {
            CmbProducts.ItemsSource = db.Products.ToList();
        }


        private void CmbPartners_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbPartners.SelectedItem is Partners selectedPartner)
            {
                // Заполняем поля информации о партнере
                TxtPartnerType.Text = selectedPartner.PartnerTypes.TypeName;
                TxtDirector.Text = selectedPartner.Director;
                TxtPhone.Text = selectedPartner.Phone;
                TxtEmail.Text = selectedPartner.Email;
                TxtAddress.Text = selectedPartner.Address;
                TxtRating.Text = selectedPartner.Rating?.ToString() ?? "Не указан";
            }
        }


        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что товар выбран
            if (CmbProducts.SelectedItem is Products selectedProduct)
            {
                // Проверяем корректность введенного количества
                if (int.TryParse(TxtQuantity.Text, out int quantity) && quantity > 0)
                {
                    // Проверяем, есть ли уже такой товар в заявке
                    var existingItem = requestItems.FirstOrDefault(x => x.ProductID == selectedProduct.ID);

                    if (existingItem != null)
                    {
                        // Если товар уже есть - увеличиваем количество
                        existingItem.Quantity += quantity;
                        existingItem.Total = existingItem.Quantity * existingItem.Price;
                    }
                    else
                    {
                        // Если товара нет - добавляем новый
                        requestItems.Add(new RequestItemViewModel
                        {
                            ProductID = selectedProduct.ID,
                            ProductName = selectedProduct.ProductName,
                            Quantity = quantity,
                            Price = selectedProduct.MinPrice ?? 0,
                            Total = quantity * (selectedProduct.MinPrice ?? 0)
                        });
                    }

                    // Обновляем отображение списка товаров
                    DgRequestItems.ItemsSource = null;
                    DgRequestItems.ItemsSource = requestItems;

                    // Пересчитываем общую сумму
                    UpdateTotalAmount();

                    // Сбрасываем поле количества к значению по умолчанию
                    TxtQuantity.Text = "1";
                }
                else
                {
                    MessageBox.Show("Введите корректное количество");
                }
            }
            else
            {
                MessageBox.Show("Выберите товар");
            }
        }


        private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            // Получаем товар из контекста кнопки
            if (sender is Button button && button.DataContext is RequestItemViewModel item)
            {
                // Удаляем товар из списка
                requestItems.Remove(item);

                // Обновляем отображение списка товаров
                DgRequestItems.ItemsSource = null;
                DgRequestItems.ItemsSource = requestItems;

                // Пересчитываем общую сумму
                UpdateTotalAmount();
            }
        }


        private void UpdateTotalAmount()
        {
            decimal total = requestItems.Sum(x => x.Total);
            TxtTotalAmount.Text = $"{total:N2} ₽";
        }


        private void BtnSaveRequest_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что выбран партнер
            if (CmbPartners.SelectedItem == null)
            {
                MessageBox.Show("Выберите партнера", "Ошибка");
                return;
            }

            // Проверяем, что в заявке есть товары
            if (requestItems.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один товар в заявку", "Ошибка");
                return;
            }

            try
            {
                var partner = CmbPartners.SelectedItem as Partners;

                // Создаем новую заявку
                var newRequest = new PartnerRequests
                {
                    PartnerID = partner.ID,
                    RequestDate = DateTime.Now // Устанавливаем текущую дату
                };

                // Добавляем заявку в БД и сохраняем, чтобы получить ID
                db.PartnerRequests.Add(newRequest);
                db.SaveChanges();

                // Добавляем все товары заявки
                foreach (var item in requestItems)
                {
                    var requestItem = new RequestItems
                    {
                        RequestID = newRequest.ID, // Связываем с созданной заявкой
                        ProductID = item.ProductID,
                        Quantity = item.Quantity
                    };
                    db.RequestItems.Add(requestItem);
                }

                // Сохраняем товары заявки
                db.SaveChanges();

                MessageBox.Show("Заявка успешно создана!");
                this.Close(); // Закрываем окно после успешного сохранения
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения заявки: {ex.Message}");
            }
        }


        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }


    public class RequestItemViewModel
    {
        public int ProductID { get; set; }      // ID товара
        public string ProductName { get; set; } // Название товара
        public int Quantity { get; set; }       // Количество
        public decimal Price { get; set; }      // Цена за единицу
        public decimal Total { get; set; }      // Общая стоимость (Quantity * Price)
    }
}