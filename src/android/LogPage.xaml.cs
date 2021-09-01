using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает страницу журнала программы
	/// </summary>
	[XamlCompilation (XamlCompilationOptions.Compile)]
	public partial class LogPage: ContentPage
		{
		private Button[] buttons = new Button[2];
		private Grid mainGrid;

		/// <summary>
		/// Конструктор. Запускает страницу
		/// </summary>
		public LogPage ()
			{
			InitializeComponent ();
			buttons[0] = (Button)this.FindByName ("AllNewsButton");
			buttons[1] = (Button)this.FindByName ("GetGMJ");
			mainGrid = (Grid)this.FindByName ("MainGrid");
			}

		/// <summary>
		/// Обработчик события изменения ориентации экрана
		/// </summary>
		protected override void OnSizeAllocated (double width, double height)
			{
			base.OnSizeAllocated (width, height);
			buttons[0].HeightRequest = buttons[1].HeightRequest = (width > height) ? 40 : 85;
			buttons[0].WidthRequest = buttons[1].WidthRequest = (width < height) ? 35 : 85;
			mainGrid.ColumnDefinitions[0].Width = (width > height) ? 85 : 35;
			}
		}
	}
