using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace demo0202
{

    public partial class ProductsCatalogWindow : Window
    {
        // Подключение к базе данных
        SemyonovaDemo0202Entities db = new SemyonovaDemo0202Entities();

        public ProductsCatalogWindow()
        {
            InitializeComponent();
            // Подписываемся на событие загрузки окна
            Loaded += ProductsCatalogWindow_Loaded;
        }


        private void ProductsCatalogWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadFilters();      // Загружаем данные для фильтров
                LoadProducts();     // Загружаем список товаров
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка");
            }
        }


        private void LoadFilters()
        {
            // Загружаем типы продукции для фильтра по типу
            CmbProductTypeFilter.ItemsSource = db.ProductTypes.ToList();

            // Загружаем материалы для фильтра по материалу
            CmbMaterialFilter.ItemsSource = db.Materials.ToList();
        }


        private void LoadProducts()
        {
            // Начинаем с базового запроса ко всем товарам
            var products = db.Products.AsQueryable();

            // Фильтр по типу продукции
            if (CmbProductTypeFilter.SelectedItem is ProductTypes productType)
            {
                products = products.Where(p => p.ProductTypeID == productType.ID);
            }

            // Фильтр по материалу
            if (CmbMaterialFilter.SelectedItem is Materials material)
            {
                products = products.Where(p => p.MaterialID == material.ID);
            }

            // Применяем фильтры и отображаем результат в DataGrid
            DgProducts.ItemsSource = products.ToList();

            // Показываем количество найденных товаров
            TxtProductCount.Text = $"Найдено товаров: {products.Count()}";
        }


        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadProducts(); // Перезагружаем товары с новыми фильтрами
        }


        private void BtnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            // Сбрасываем выбранные значения фильтров
            CmbProductTypeFilter.SelectedItem = null;
            CmbMaterialFilter.SelectedItem = null;

            // Загружаем все товары без фильтров
            LoadProducts();
        }
    }
}