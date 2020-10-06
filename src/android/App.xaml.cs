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

		private SupportedLanguages al = Localization.CurrentLanguage;
		private NotificationsSet ns = new NotificationsSet ();
		private int currentNotification = 0;

		private readonly Color
			logMasterBackColor = Color.FromHex ("#F0F0F0"),
			logFieldBackColor = Color.FromHex ("#D0D0D0"),

			solutionMasterBackColor = Color.FromHex ("#F0F8FF"),
			solutionFieldBackColor = Color.FromHex ("#D0E8FF"),

			aboutMasterBackColor = Color.FromHex ("#F0FFF0"),
			aboutFieldBackColor = Color.FromHex ("#D0FFD0"),

			masterTextColor = Color.FromHex ("#000080"),
			masterHeaderColor = Color.FromHex ("#202020");

		#endregion

		#region Переменные страниц

		private ContentPage solutionPage, aboutPage, logPage;
		private Label aboutLabel, freqFieldLabel, occFieldLabel;
		private Switch allowStart, allowSound, allowLight, allowVibro, allowOnLockedScreen, enabledSwitch;
		private Button selectedNotification, applyButton, addButton, deleteButton;
		private Editor nameField, linkField, beginningField, endingField, mainLog;
		private uint currentOcc, currentFreq;

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
			if ((ButtonTitle != "+") && (ButtonTitle != "–"))
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
		/// <param name="CurrentTabNumber">Номер текущей вкладки при старте</param>
		public App (uint CurrentTabNumber)
			{
			// Инициализация
			InitializeComponent ();

			// Общая конструкция страниц приложения
			MainPage = new MasterPage ();

			solutionPage = ApplyPageSettings ("SolutionPage", solutionMasterBackColor);
			aboutPage = ApplyPageSettings ("AboutPage", aboutMasterBackColor);
			logPage = ApplyPageSettings ("LogPage", logMasterBackColor);

			if (CurrentTabNumber > 1)
				CurrentTabNumber = 1;
			((CarouselPage)MainPage).CurrentPage = ((CarouselPage)MainPage).Children[(int)CurrentTabNumber];

			#region Настройки службы

			ApplyLabelSettings (solutionPage, "ServiceSettingsLabel", Localization.GetText ("ServiceSettingsLabel", al),
				masterHeaderColor);

			ApplyLabelSettings (solutionPage, "AllowStartLabel", Localization.GetText ("AllowStartSwitch", al),
				masterTextColor);
			allowStart = (Switch)solutionPage.FindByName ("AllowStart");
			allowStart.IsToggled = NotificationsSupport.AllowServiceToStart;
			allowStart.Toggled += AllowStart_Toggled;

			ApplyLabelSettings (solutionPage, "AlarmSettingsLabel", Localization.GetText ("AlarmSettingsLabel", al),
				masterHeaderColor);

			ApplyLabelSettings (solutionPage, "AllowSoundLabel", Localization.GetText ("AllowSoundSwitch", al),
				masterTextColor);
			allowSound = (Switch)solutionPage.FindByName ("AllowSound");
			allowSound.IsToggled = NotificationsSupport.AllowSound;
			allowSound.Toggled += AllowSound_Toggled;

			ApplyLabelSettings (solutionPage, "AllowLightLabel", Localization.GetText ("AllowLightSwitch", al),
				masterTextColor);
			allowLight = (Switch)solutionPage.FindByName ("AllowLight");
			allowLight.IsToggled = NotificationsSupport.AllowLight;
			allowLight.Toggled += AllowLight_Toggled;

			ApplyLabelSettings (solutionPage, "AllowVibroLabel", Localization.GetText ("AllowVibroSwitch", al),
				masterTextColor);
			allowVibro = (Switch)solutionPage.FindByName ("AllowVibro");
			allowVibro.IsToggled = NotificationsSupport.AllowVibro;
			allowVibro.Toggled += AllowVibro_Toggled;

			ApplyLabelSettings (solutionPage, "AllowOnLockedScreenLabel", Localization.GetText ("AllowOnLockedScreenSwitch", al),
				masterTextColor);
			allowOnLockedScreen = (Switch)solutionPage.FindByName ("AllowOnLockedScreen");
			allowOnLockedScreen.IsToggled = NotificationsSupport.AllowOnLockedScreen;
			allowOnLockedScreen.Toggled += AllowOnLockedScreen_Toggled;

			#endregion

			#region Настройки оповещений

			ApplyLabelSettings (solutionPage, "NotificationsSettingsLabel",
				Localization.GetText ("NotificationsSettingsLabel", al), masterHeaderColor);
			selectedNotification = ApplyButtonSettings (solutionPage, "SelectedNotification",
				"", solutionFieldBackColor, SelectNotification);

			ApplyLabelSettings (solutionPage, "NameFieldLabel", Localization.GetText ("NameFieldLabel", al),
				masterTextColor);
			nameField = ApplyEditorSettings (solutionPage, "NameField", solutionFieldBackColor,
				Keyboard.Default, Notification.MaxBeginningEndingLength, "", null);

			ApplyLabelSettings (solutionPage, "LinkFieldLabel", Localization.GetText ("LinkFieldLabel", al),
				masterTextColor);
			linkField = ApplyEditorSettings (solutionPage, "LinkField", solutionFieldBackColor,
				Keyboard.Url, 150, "", null);

			ApplyLabelSettings (solutionPage, "BeginningFieldLabel", Localization.GetText ("BeginningFieldLabel", al),
				masterTextColor);
			beginningField = ApplyEditorSettings (solutionPage, "BeginningField", solutionFieldBackColor,
				Keyboard.Url, Notification.MaxBeginningEndingLength, "", null);

			ApplyLabelSettings (solutionPage, "EndingFieldLabel", Localization.GetText ("EndingFieldLabel", al),
				masterTextColor);
			endingField = ApplyEditorSettings (solutionPage, "EndingField", solutionFieldBackColor,
				Keyboard.Url, Notification.MaxBeginningEndingLength, "", null);

			freqFieldLabel = ApplyLabelSettings (solutionPage, "FreqFieldLabel", "", masterTextColor);
			ApplyButtonSettings (solutionPage, "FreqIncButton", "+", solutionFieldBackColor, FrequencyChanged);
			ApplyButtonSettings (solutionPage, "FreqDecButton", "–", solutionFieldBackColor, FrequencyChanged);
			currentFreq = 1;

			occFieldLabel = ApplyLabelSettings (solutionPage, "OccFieldLabel", "", masterTextColor);
			ApplyButtonSettings (solutionPage, "OccIncButton", "+", solutionFieldBackColor, OccurenceChanged);
			ApplyButtonSettings (solutionPage, "OccDecButton", "–", solutionFieldBackColor, OccurenceChanged);
			currentOcc = 1;

			enabledSwitch = (Switch)solutionPage.FindByName ("EnabledSwitch");

			// Инициализация полей
			SelectNotification (null, null);

			#endregion

			#region Управление оповещениями

			applyButton = ApplyButtonSettings (solutionPage, "ApplyButton", Localization.GetText ("ApplyButton", al),
				solutionFieldBackColor, ApplyNotification);
			addButton = ApplyButtonSettings (solutionPage, "AddButton", Localization.GetText ("AddButton", al),
				solutionFieldBackColor, AddNotification);
			deleteButton = ApplyButtonSettings (solutionPage, "DeleteButton", Localization.GetText ("DeleteButton", al),
				solutionFieldBackColor, DeleteNotification);

			ApplyButtonSettings (solutionPage, "TemplateButton", Localization.GetText ("TemplateButton", al),
				solutionFieldBackColor, LoadTemplate);
			ApplyButtonSettings (solutionPage, "FindDelimitersButton", Localization.GetText ("FindDelimitersButton", al),
				solutionFieldBackColor, FindDelimiters);

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
			ApplyButtonSettings (aboutPage, "ManualPage", Localization.GetText ("ManualPage", al),
				aboutFieldBackColor, ManualButton_Clicked);
			ApplyButtonSettings (aboutPage, "ADPPage", Localization.GetText ("ADPPage", al),
				aboutFieldBackColor, ADPButton_Clicked);
			ApplyButtonSettings (aboutPage, "CommunityPage",
				"RD AAOW Free utilities production lab", aboutFieldBackColor, CommunityButton_Clicked);

			UpdateButtons ();

			ApplyButtonSettings (aboutPage, "LanguageSelector", Localization.LanguagesNames[(int)al],
				aboutFieldBackColor, SelectLanguage_Clicked);

			#endregion

			#region Страница лога приложения

			mainLog = ApplyEditorSettings (logPage, "MainLog", logFieldBackColor, Keyboard.Text, 10000,
				NotificationsSupport.MasterLog, null);
			mainLog.IsReadOnly = true;

			ApplyButtonSettings (logPage, "LogNotification", Localization.GetText ("LogNotification", al),
				logFieldBackColor, SelectLogNotification);

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

		// Включение / выключение светодиода
		private void AllowLight_Toggled (object sender, ToggledEventArgs e)
			{
			NotificationsSupport.AllowLight = allowLight.IsToggled;
			}

		// Включение / выключение светодиода
		private void AllowVibro_Toggled (object sender, ToggledEventArgs e)
			{
			NotificationsSupport.AllowVibro = allowVibro.IsToggled;
			}

		// Включение / выключение отображения текста сообщений на заблокированном экране
		private void AllowOnLockedScreen_Toggled (object sender, ToggledEventArgs e)
			{
			NotificationsSupport.AllowOnLockedScreen = allowOnLockedScreen.IsToggled;
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
			if (NotificationsSupport.HelpShownAt != "")
				return;

			// Требование принятия Политики
			while (await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					Localization.GetText ("PolicyMessage", al),
					Localization.GetText ("DeclineButton", al),
					Localization.GetText ("AcceptButton", al)))
				{
				ADPButton_Clicked (null, null);
				}
			NotificationsSupport.HelpShownAt = ProgramDescription.AssemblyVersion; // Только после принятия

			// Первая подсказка
			await solutionPage.DisplayAlert (Localization.GetText ("TipHeader01", al),
				Localization.GetText ("Tip01", al), Localization.GetText ("NextButton", al));

			await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
				Localization.GetText ("Tip04", al), Localization.GetText ("NextButton", al));

			await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
				Localization.GetText ("Tip02", al), Localization.GetText ("NextButton", al));
			}

		// Страница проекта
		private void AppButton_Clicked (object sender, EventArgs e)
			{
			Launcher.OpenAsync ("https://github.com/adslbarxatov/" + ProgramDescription.AssemblyMainName);
			}

		// Страница видеоруководства
		private void ManualButton_Clicked (object sender, EventArgs e)
			{
			Launcher.OpenAsync ("https://www.youtube.com/watch?v=QqNsfbzw6sE");
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

		// Выбор оповещения
		private async void SelectNotification (object sender, EventArgs e)
			{
			// Запрос списка оповещений
			List<string> list = new List<string> ();
			foreach (Notification element in ns.Notifications)
				list.Add (element.Name);

			string res = list[currentNotification];
			if (e != null)
				res = await solutionPage.DisplayActionSheet (Localization.GetText ("SelectNotification", al),
					Localization.GetText ("CancelButton", al), null, list.ToArray ());

			// Установка результата
			int i = currentNotification;
			if ((e == null) || ((i = list.IndexOf (res)) >= 0))
				{
				currentNotification = i;
				selectedNotification.Text = res;

				nameField.Text = ns.Notifications[i].Name;
				linkField.Text = ns.Notifications[i].Link;
				beginningField.Text = ns.Notifications[i].Beginning;
				endingField.Text = ns.Notifications[i].Ending;
				currentFreq = ns.Notifications[i].UpdateFrequency;
				FrequencyChanged (null, null);
				currentOcc = ns.Notifications[i].OccurenceNumber;
				OccurenceChanged (null, null);
				enabledSwitch.IsToggled = ns.Notifications[i].IsEnabled;
				}

			// Сброс
			list.Clear ();
			}

		// Выбор оповещения для перехода по ссылке
		private async void SelectLogNotification (object sender, EventArgs e)
			{
			List<string> list = new List<string> ();
			foreach (Notification element in ns.Notifications)
				list.Add (element.Name);

			string res = await solutionPage.DisplayActionSheet (Localization.GetText ("SelectNotification", al),
				Localization.GetText ("CancelButton", al), null, list.ToArray ());

			int i;
			if ((i = list.IndexOf (res)) >= 0)
				{
				try
					{
					await Launcher.OpenAsync (ns.Notifications[i].Link);
					}
				catch
					{
					}
				}

			// Сброс
			list.Clear ();
			}

		// Изменение значения частоты опроса
		private void FrequencyChanged (object sender, EventArgs e)
			{
			if (sender != null)
				{
				Button b = (Button)sender;
				if ((b.Text == "+") && (currentFreq < 24))
					currentFreq++;
				else if ((b.Text == "–") && (currentFreq > 1))
					currentFreq--;
				}

			freqFieldLabel.Text = string.Format (Localization.GetText ("FreqFieldLabel", al),
				currentFreq * 10 * NotificationsSet.MaxNotifications / 60);
			}

		// Изменение порядкового номера вхождения
		private void OccurenceChanged (object sender, EventArgs e)
			{
			if (sender != null)
				{
				Button b = (Button)sender;
				if ((b.Text == "+") && (currentOcc < Notification.MaxOccurenceNumber))
					currentOcc++;
				else if ((b.Text == "–") && (currentOcc > 1))
					currentOcc--;
				}

			occFieldLabel.Text = string.Format (Localization.GetText ("OccFieldLabel", al), currentOcc);
			}

		// Удаление оповещения
		private async void DeleteNotification (object sender, EventArgs e)
			{
			// Контроль
			if (!await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
				Localization.GetText ("DeleteMessage", al), Localization.GetText ("NextButton", al),
				Localization.GetText ("CancelButton", al)))
				return;

			// Удаление и переход к другому оповещению
			ns.Notifications.RemoveAt (currentNotification);
			if (currentNotification >= ns.Notifications.Count)
				currentNotification = ns.Notifications.Count - 1;
			SelectNotification (null, null);

			// Обновление контролов
			UpdateButtons ();
			}

		// Добавление нового оповещения
		private void AddNotification (object sender, EventArgs e)
			{
			// Добавление
			UpdateItem (-1);

			// Выбор нового оповещения
			if (itemUpdated)
				{
				currentNotification = ns.Notifications.Count - 1;
				SelectNotification (null, null);
				}
			}

		// Обновление оповещения
		private void ApplyNotification (object sender, EventArgs e)
			{
			// Обновление
			UpdateItem (currentNotification);

			if (itemUpdated)
				selectedNotification.Text = nameField.Text;
			}

		// Общий метод обновления оповещений
		private bool itemUpdated = false;
		private async void UpdateItem (int ItemNumber)
			{
			// Инициализация оповещения
			Notification ni = new Notification (nameField.Text, linkField.Text, beginningField.Text, endingField.Text,
				currentFreq, currentOcc);

			if (!ni.IsInited)
				{
				await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					Localization.GetText ("NotEnoughDataMessage", al), Localization.GetText ("NextButton", al));

				itemUpdated = false;
				nameField.Focus ();
				return;
				}
			if ((ItemNumber < 0) && ns.Notifications.Contains (ni)) // Не относится к обновлению позиции
				{
				await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					Localization.GetText ("NotMatchingNames", al), Localization.GetText ("NextButton", al));

				itemUpdated = false;
				nameField.Focus ();
				return;
				}

			ni.IsEnabled = enabledSwitch.IsToggled;

			// Добавление
			if (ItemNumber < 0)
				{
				ns.Notifications.Add (ni);
				}
			else if (ItemNumber < ns.Notifications.Count)
				{
				ns.Notifications[ItemNumber] = ni;
				}

			// Обновление контролов
			UpdateButtons ();
			itemUpdated = true;
			}

		// Обновление кнопок
		private void UpdateButtons ()
			{
			addButton.IsVisible = (ns.Notifications.Count < NotificationsSet.MaxNotifications);
			deleteButton.IsVisible = (ns.Notifications.Count > 1);
			}

		// Метод загружает шаблон оповещения
		private List<string> templatesNames = new List<string> ();
		private async void LoadTemplate (object sender, EventArgs e)
			{
			// Запрос списка шаблонов
			if (templatesNames.Count == 0)
				for (uint i = 0; i < ns.NotificationsTemplates.TemplatesCount; i++)
					templatesNames.Add (ns.NotificationsTemplates.GetName (i));

			string res = await solutionPage.DisplayActionSheet (Localization.GetText ("SelectTemplate", al),
				Localization.GetText ("CancelButton", al), null, templatesNames.ToArray ());

			// Установка результата
			uint templateNumber = 0;
			int r;
			if ((r = templatesNames.IndexOf (res)) >= 0)
				templateNumber = (uint)r;

			// Проверка
			if (ns.NotificationsTemplates.IsTemplateIncomplete (templateNumber))
				await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					Localization.GetText ("CurlyTemplate", al), Localization.GetText ("NextButton", al));

			// Заполнение
			nameField.Text = ns.NotificationsTemplates.GetName (templateNumber);
			linkField.Text = ns.NotificationsTemplates.GetLink (templateNumber);
			beginningField.Text = ns.NotificationsTemplates.GetBeginning (templateNumber);
			endingField.Text = ns.NotificationsTemplates.GetEnding (templateNumber);
			currentOcc = ns.NotificationsTemplates.GetOccurenceNumber (templateNumber);
			OccurenceChanged (null, null);
			}

		// Автоматизированный поиск ограничителей
		private async void FindDelimiters (object sender, EventArgs e)
			{
			// Контроль
			beginningField.Focus ();

			if (beginningField.Text == "")
				{
				await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					Localization.GetText ("KeywordNotSpecified", al), Localization.GetText ("NextButton", al));
				return;
				}

			if (beginningField.Text.Contains ("<") || beginningField.Text.Contains (">"))
				{
				await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					Localization.GetText ("Tip03", al), Localization.GetText ("NextButton", al));
				beginningField.Text = "";
				return;
				}

			// Поиск
			string beginning = "", ending = "";
			if (!Notification.FindDelimiters (linkField.Text, beginningField.Text, out beginning, out ending))
				{
				await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					Localization.GetText ("SearchFailure", al), Localization.GetText ("NextButton", al));
				return;
				}

			// Успешно
			beginningField.Text = beginning.Trim ();
			endingField.Text = ending.Trim ();
			}

		/// <summary>
		/// Сохранение настроек программы
		/// </summary>
		protected override void OnSleep ()
			{
			// Сохранение настроек
			if (!allowStart.IsToggled)
				{
				// Сброс текстов при отключении приложения, чтобы обеспечить повтор новостей
				// при следующем запуске. Позволяет не отображать повторно все новости при вызове
				// приложения для просмотра полных текстов
				foreach (Notification n in ns.Notifications)
					n.ResetTimer ();
				}
			ns.SaveNotifications ();

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
