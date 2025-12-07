using System;
using System.Linq;
using System.Windows;

namespace demo0202
{
    public partial class CalculationsWindow : Window
    {
        SemyonovaDemo0202Entities db = new SemyonovaDemo0202Entities();

        public CalculationsWindow()
        {
            InitializeComponent();
            Loaded += CalculationsWindow_Loaded;
        }

        private void CalculationsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                CmbProductType.ItemsSource = db.ProductTypes.ToList();
                CmbMaterialType.ItemsSource = db.MaterialTypes.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CmbProductType.SelectedItem == null || CmbMaterialType.SelectedItem == null)
                {
                    MessageBox.Show("Выберите тип продукции и тип материала");
                    return;
                }

                var productType = CmbProductType.SelectedItem as ProductTypes;
                var materialType = CmbMaterialType.SelectedItem as MaterialTypes;

                if (!int.TryParse(TxtStockQuantity.Text, out int stockQty) ||
                    !int.TryParse(TxtRequiredQuantity.Text, out int requiredQty) ||
                    !double.TryParse(TxtParam1.Text, out double param1) ||
                    !double.TryParse(TxtParam2.Text, out double param2))
                {
                    MessageBox.Show("Введите корректные числовые значения");
                    return;
                }

                if (stockQty < 0 || requiredQty < 0 || param1 <= 0 || param2 <= 0)
                {
                    MessageBox.Show("Все значения должны быть положительными");
                    return;
                }

                // Вызываем метод расчета
                int result = CalculateMaterialRequirement(productType.ID, materialType.ID, requiredQty, stockQty, param1, param2
                );

                if (result == -1)
                {
                    TxtResult.Text = "Ошибка расчета: проверьте входные данные";
                }
                else
                {
                    // Рассчитываем сколько материала без учета брака (для информации)
                    int materialWithoutLoss = 0;
                    if (result > 0)
                    {
                        double coeff = materialType.LossCoefficient ?? 0;
                        materialWithoutLoss = (int)Math.Ceiling(result / (1 + coeff / 100.0));
                    }

                    TxtResult.Text = $"Требуется материалов: {result:N0} единиц\n";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка расчета: {ex.Message}");
            }
        }


        private int CalculateMaterialRequirement(int productTypeId, int materialTypeId, int requiredQuantity, int stockQuantity, double param1, double param2)
        {
            try
            {
                // Находим необходимые в базе переменные
                ProductTypes productType = db.ProductTypes.FirstOrDefault(x => x.ID == productTypeId);
                MaterialTypes materialType = db.MaterialTypes.FirstOrDefault(x => x.ID == materialTypeId);

                // Проверяем существование типов и корректность входных данных
                if (productType == null || materialType == null ||
                    requiredQuantity < 0 || stockQuantity < 0 ||
                    param1 <= 0 || param2 <= 0)
                {
                    return -1;
                }

                // Проверяем, что коэффициенты не равны null
                if (productType.LossCoefficient == null || materialType.LossCoefficient == null)
                {
                    return -1;
                }

                // Получаем коэффициенты
                double coeffProduct = productType.LossCoefficient.Value;
                double coeffMaterial = materialType.LossCoefficient.Value;

                // Сколько нужно произвести (учитываем склад)
                int productionNeeded = Math.Max(0, requiredQuantity - stockQuantity);

                if (productionNeeded == 0)
                {
                    return 0;
                }

                // Материал на одну единицу продукции
                double materialPerUnit = param1 * param2 * coeffProduct;

                // Учитываем брак материала
                double materialWithLoss = materialPerUnit * (1 + coeffMaterial / 100.0);

                // Общее количество материала
                double totalMaterial = materialWithLoss * productionNeeded;

                // Округляем до целого вверх
                return (int)Math.Ceiling(totalMaterial);
            }
            catch
            {
                return -1;
            }
        }
    }
}