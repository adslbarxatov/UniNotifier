using System;
using System.Collections.Generic;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает функционал приложения
	/// </summary>
	public partial class App:Application
		{
		#region Общие переменные и константы

		private const int masterFontSize = 14;
		private Thickness margin = new Thickness (6);
		private bool firstStart = true;
		private SupportedLanguages al = Localization.CurrentLanguage;

		private readonly Color
			solutionMasterBackColor = Color.FromHex ("#F0FFF0"),
			solutionFieldBackColor = Color.FromHex ("#D0FFD0"),

			aboutMasterBackColor = Color.FromHex ("#F0FFF0"),
			aboutFieldBackColor = Color.FromHex ("#D0FFD0"),

			masterTextColor = Color.FromHex ("#000080"),
			masterHeaderColor = Color.FromHex ("#202020");

		private const string firstStartRegKey = "HelpShownAt";

		#endregion

		#region Переменные страниц

		private ContentPage solutionPage, aboutPage;

		private Label aboutLabel;

		private Switch allowStart, allowSound;

		#endregion

		#region Вспомогательные методы

		private ContentPage ApplyPageSettings (string PageName, Color PageBackColor)
			{
			// Инициализация страницы
			ContentPage page = (ContentPage)MainPage.FindByName (PageName);
			page.Title = Localization.GetText (PageName, al);
			page.BackgroundColor = PageBackColor;

			ApplyHeaderLabelSettings (page, page.Title, PageBackColor);

			return page;
			}

		private Label ApplyLabelSettings (ContentPage ParentPage, string LabelName,
			string LabelTitle, Color LabelTextColor)
			{
			Label childLabel = (Label)ParentPage.FindByName (LabelName);

			childLabel.Text = LabelTitle;
			childLabel.HorizontalOptions = LayoutOptions.Center;
			childLabel.FontAttributes = FontAttributes.Bold;
			childLabel.FontSize = masterFontSize;
			childLabel.TextColor = LabelTextColor;
			childLabel.Margin = margin;

			return childLabel;
			}

		private Button ApplyButtonSettings (ContentPage ParentPage, string ButtonName,
			string ButtonTitle, Color ButtonColor, EventHandler ButtonMethod)
			{
			Button childButton = (Button)ParentPage.FindByName (ButtonName);

			childButton.BackgroundColor = ButtonColor;
			childButton.FontAttributes = FontAttributes.None;
			childButton.FontSize = masterFontSize;
			childButton.TextColor = masterTextColor;
			childButton.Margin = margin;
			childButton.Text = ButtonTitle;
			if (ButtonMethod != null)
				childButton.Clicked += ButtonMethod;

			return childButton;
			}

		private Editor ApplyEditorSettings (ContentPage ParentPage, string EditorName,
			Color EditorColor, Keyboard EditorKeyboard, uint MaxLength,
			string InitialText, EventHandler<TextChangedEventArgs> EditMethod)
			{
			Editor childEditor = (Editor)ParentPage.FindByName (EditorName);

			childEditor.AutoSize = EditorAutoSizeOption.TextChanges;
			childEditor.BackgroundColor = EditorColor;
			childEditor.FontAttributes = FontAttributes.None;
			childEditor.FontFamily = "Serif";
			childEditor.FontSize = masterFontSize;
			childEditor.Keyboard = EditorKeyboard;
			childEditor.MaxLength = (int)MaxLength;
			//childEditor.Placeholder = "...";
			//childEditor.PlaceholderColor = Color.FromRgb (255, 255, 0);
			childEditor.TextColor = masterTextColor;
			childEditor.Margin = margin;

			childEditor.Text = InitialText;
			childEditor.TextChanged += EditMethod;

			return childEditor;
			}

		private void ApplyHeaderLabelSettings (ContentPage ParentPage, string LabelTitle, Color BackColor)
			{
			Label childLabel = (Label)ParentPage.FindByName ("HeaderLabel");

			childLabel.BackgroundColor = masterHeaderColor;
			childLabel.FontAttributes = FontAttributes.Bold;
			childLabel.FontSize = masterFontSize;
			childLabel.HorizontalTextAlignment = TextAlignment.Center;
			childLabel.HorizontalOptions = LayoutOptions.Fill;
			childLabel.Padding = margin;
			childLabel.Text = LabelTitle;
			childLabel.TextColor = BackColor;
			}

		#endregion

		/// <summary>
		/// Конструктор. Точка входа приложения
		/// </summary>
		public App ()
			{
			// Инициализация
			InitializeComponent ();

			// Общая конструкция страниц приложения
			MainPage = new MasterPage ();

			solutionPage = ApplyPageSettings ("SolutionPage", solutionMasterBackColor);
			aboutPage = ApplyPageSettings ("AboutPage", aboutMasterBackColor);

			#region Основная страница

			ApplyLabelSettings (solutionPage, "AllowStartLabel", Localization.GetText ("AllowStartSwitch", al),
				masterTextColor);
			allowStart = (Switch)solutionPage.FindByName ("AllowStart");
			allowStart.IsToggled = NotificationsSupport.AllowServiceToStart;
			allowStart.Toggled += AllowStart_Toggled;

			ApplyLabelSettings (solutionPage, "AllowSoundLabel", Localization.GetText ("AllowSoundSwitch", al),
				masterTextColor);
			allowSound = (Switch)solutionPage.FindByName ("AllowSound");
			allowSound.IsToggled = NotificationsSupport.AllowSound;
			allowSound.Toggled += AllowSound_Toggled;

			// Получение настроек перед инициализацией
			try
				{
				firstStart = Preferences.Get (firstStartRegKey, "") == "";
				}
			catch
				{
				}

			#endregion

			#region Страница "О программе"

			aboutLabel = ApplyLabelSettings (aboutPage, "AboutLabel",
				ProgramDescription.AssemblyTitle + "\n" +
				ProgramDescription.AssemblyDescription + "\n\n" +
				ProgramDescription.AssemblyCopyright + "\nv " +
				ProgramDescription.AssemblyVersion +
				"; " + ProgramDescription.AssemblyLastUpdate,
				Color.FromHex ("#000080"));
			aboutLabel.FontAttributes = FontAttributes.Bold;
			aboutLabel.HorizontalOptions = LayoutOptions.Fill;
			aboutLabel.HorizontalTextAlignment = TextAlignment.Center;

			ApplyButtonSettings (aboutPage, "AppPage", Localization.GetText ("AppPage", al),
				aboutFieldBackColor, AppButton_Clicked);
			ApplyButtonSettings (aboutPage, "ADPPage", Localization.GetText ("ADPPage", al),
				aboutFieldBackColor, ADPButton_Clicked);
			ApplyButtonSettings (aboutPage, "CommunityPage",
				"RD AAOW Free utilities production lab", aboutFieldBackColor, CommunityButton_Clicked);

			ApplyButtonSettings (aboutPage, "LanguageSelector", Localization.LanguagesNames[(int)al],
				aboutFieldBackColor, SelectLanguage_Clicked);

			#endregion

			// Принятие соглашений
			ShowTips ();
			}

		// Включение / выключение службы
		private void AllowStart_Toggled (object sender, ToggledEventArgs e)
			{
			NotificationsSupport.AllowServiceToStart = allowStart.IsToggled;
			}

		// Включение / выключение звука
		private void AllowSound_Toggled (object sender, ToggledEventArgs e)
			{
			NotificationsSupport.AllowSound = allowSound.IsToggled;
			}

		// Выбор языка приложения
		private async void SelectLanguage_Clicked (object sender, EventArgs e)
			{
			// Запрос
			string res = await aboutPage.DisplayActionSheet (Localization.GetText ("SelectLanguage", al),
				Localization.GetText ("CancelButton", al), null, Localization.LanguagesNames);

			// Сохранение
			List<string> lngs = new List<string> (Localization.LanguagesNames);
			if (lngs.Contains (res))
				{
				al = (SupportedLanguages)lngs.IndexOf (res);
				await aboutPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					Localization.GetText ("RestartApp", al), "OK");
				}
			}

		// Метод отображает подсказки при первом запуске
		private async void ShowTips ()
			{
			if (!firstStart)
				return;

			// Требование принятия Политики
			while (await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					Localization.GetText ("PolicyMessage", al),
					Localization.GetText ("DeclineButton", al),
					Localization.GetText ("AcceptButton", al)))
				{
				ADPButton_Clicked (null, null);
				}
			Preferences.Set (firstStartRegKey, ProgramDescription.AssemblyVersion); // Только после принятия

			// Первая подсказка
			await solutionPage.DisplayAlert (Localization.GetText ("TipHeader01", al),
				Localization.GetText ("Tip01", al), Localization.GetText ("NextButton", al));

			await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
				Localization.GetText ("Tip02", al), Localization.GetText ("NextButton", al));
			}

		// Страница проекта
		private void AppButton_Clicked (object sender, EventArgs e)
			{
			Launcher.OpenAsync ("https://github.com/adslbarxatov/" + ProgramDescription.AssemblyMainName);
			}

		// Страница лаборатории
		private void CommunityButton_Clicked (object sender, EventArgs e)
			{
			Launcher.OpenAsync ("https://vk.com/rdaaow_fupl");
			}

		// Страница политики и EULA
		private void ADPButton_Clicked (object sender, EventArgs e)
			{
			Launcher.OpenAsync ("https://vk.com/@rdaaow_fupl-adp");
			}

		/// <summary>
		/// Сохранение настроек программы
		/// </summary>
		protected override void OnSleep ()
			{
			try
				{
				Localization.CurrentLanguage = al;
				}
			catch
				{
				}
			}

		#region Стандартные обработчики

		/// <summary>
		/// Обработчик события запуска приложения
		/// </summary>
		protected override void OnStart ()
			{
			}

		/// <summary>
		/// Обработчик события выхода из ждущего режима
		/// </summary>
		protected override void OnResume ()
			{
			}

		#endregion
		}
	}
