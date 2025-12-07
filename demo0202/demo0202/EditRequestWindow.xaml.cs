using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace demo0202
{
    public partial class EditRequestWindow : Window
    {
        SemyonovaDemo0202Entities db = new SemyonovaDemo0202Entities();
        private int requestId;
        private List<RequestItemWithTotal> itemsWithTotal;

        // Делегат для обновления
        public delegate void RequestUpdatedHandler(int requestId);
        public event RequestUpdatedHandler RequestUpdated;

        public EditRequestWindow(int requestId)
        {
            InitializeComponent();
            this.requestId = requestId;
            Loaded += EditRequestWindow_Loaded;
        }

        private void EditRequestWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadRequestItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadRequestItems()
        {


            var items = db.RequestItems
                .Where(x => x.RequestID == requestId)
                .Include("Products")
                .ToList();

            itemsWithTotal = items.Select(item => new RequestItemWithTotal
            {
                RequestItem = item,
                Product = item.Products,
                Quantity = item.Quantity,
                Price = item.Products?.MinPrice ?? 0
            }).ToList();

            DgRequestItems.ItemsSource = itemsWithTotal;
        }

        private void DgRequestItems_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var cell = e.Row;
                var item = cell.Item as RequestItemWithTotal;
                if (item != null)
                {
                    // Обновляем данные в БД
                    var dbItem = db.RequestItems.Find(item.RequestItem.ID);
                    if (dbItem != null)
                    {
                        dbItem.Quantity = item.Quantity;
                    }

                    // Уведомляем об изменении суммы
                    item.NotifyPropertyChanged(nameof(item.Total));

                    // сохраняем изменения
                    try
                    {
                        db.SaveChanges();
                        RequestUpdated?.Invoke(requestId);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения: {ex.Message}");
                    }
                }
            }
        }

        private void BtnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is RequestItemWithTotal itemData)
            {
                var result = MessageBox.Show("Удалить этот товар из заявки?", "Подтверждение",
                                           MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Находим и удаляем элемент из БД
                        var dbItem = db.RequestItems.Find(itemData.RequestItem.ID);
                        if (dbItem != null)
                        {
                            db.RequestItems.Remove(dbItem);
                            db.SaveChanges();

                            // Уведомляем об изменении
                            RequestUpdated?.Invoke(requestId);

                            // Перезагружаем данные
                            LoadRequestItems();

                            MessageBox.Show("Товар удален из заявки");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}");
                    }
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Сохраняем все изменения
                db.SaveChanges();

                // Уведомляем об обновлении
                RequestUpdated?.Invoke(requestId);

                MessageBox.Show("Изменения сохранены");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class RequestItemWithTotal : INotifyPropertyChanged
    {
        private RequestItems _requestItem;
        private Products _product;
        private int? _quantity;
        private decimal _price;

        public RequestItems RequestItem
        {
            get => _requestItem;
            set
            {
                _requestItem = value;
                NotifyPropertyChanged(nameof(RequestItem));
            }
        }

        public Products Product
        {
            get => _product;
            set
            {
                _product = value;
                NotifyPropertyChanged(nameof(Product));
                NotifyPropertyChanged(nameof(Total));
            }
        }

        public int? Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    if (RequestItem != null)
                        RequestItem.Quantity = value;
                    NotifyPropertyChanged(nameof(Quantity));
                    NotifyPropertyChanged(nameof(Total));
                }
            }
        }

        public decimal Price
        {
            get => _price;
            set
            {
                _price = value;
                NotifyPropertyChanged(nameof(Price));
                NotifyPropertyChanged(nameof(Total));
            }
        }

        public string Total
        {
            get
            {
                decimal total = (_quantity ?? 0) * _price;
                return total.ToString("N2") + " ₽";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}