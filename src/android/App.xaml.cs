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
	public partial class App: Application
		{
		#region Общие переменные и константы

		private const int masterFontSize = 13;
		private Thickness margin = new Thickness (6);

		private SupportedLanguages al = Localization.CurrentLanguage;
		private NotificationsSet ns = new NotificationsSet (false);
		private int currentNotification = 0;

		private readonly Color
			logMasterBackColor = Color.FromHex ("#F0F0F0"),
			logFieldBackColor = Color.FromHex ("#80808080"),

			solutionMasterBackColor = Color.FromHex ("#F0F8FF"),
			solutionFieldBackColor = Color.FromHex ("#D0E8FF"),

			aboutMasterBackColor = Color.FromHex ("#F0FFF0"),
			aboutFieldBackColor = Color.FromHex ("#D0FFD0"),

			masterTextColor = Color.FromHex ("#000080"),
			masterHeaderColor = Color.FromHex ("#202020");

		#endregion

		#region Переменные страниц

		private ContentPage solutionPage, aboutPage, logPage;
		private Label aboutLabel, freqFieldLabel, occFieldLabel, fontSizeFieldLabel, shareOffsetLabel;
		private Switch allowStart, allowSound, allowLight, allowVibro, allowOnLockedScreen,
			enabledSwitch, readModeSwitch;
		private Button selectedNotification, applyButton, addButton, deleteButton, getGMJButton,
			shareButton, shareOffsetButton, notificationButton;
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
			childButton.TextTransform = TextTransform.None;
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
		/// <param name="AllowNotificationSettings">Флаг, указывающий на возможность настройки оповещений из приложения</param>
		public App (uint CurrentTabNumber, bool AllowNotificationSettings)
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

			if (AllowNotificationSettings)
				ApplyLabelSettings (solutionPage, "AlarmSettingsLabel", Localization.GetText ("AlarmSettingsLabel", al),
				masterHeaderColor);

			if (AllowNotificationSettings)
				ApplyLabelSettings (solutionPage, "AllowSoundLabel", Localization.GetText ("AllowSoundSwitch", al),
				masterTextColor);
			allowSound = (Switch)solutionPage.FindByName ("AllowSound");
			allowSound.IsToggled = NotificationsSupport.AllowSound;
			allowSound.Toggled += AllowSound_Toggled;
			allowSound.IsVisible = AllowNotificationSettings;

			if (AllowNotificationSettings)
				ApplyLabelSettings (solutionPage, "AllowLightLabel", Localization.GetText ("AllowLightSwitch", al),
				masterTextColor);
			allowLight = (Switch)solutionPage.FindByName ("AllowLight");
			allowLight.IsToggled = NotificationsSupport.AllowLight;
			allowLight.Toggled += AllowLight_Toggled;
			allowLight.IsVisible = AllowNotificationSettings;

			if (AllowNotificationSettings)
				ApplyLabelSettings (solutionPage, "AllowVibroLabel", Localization.GetText ("AllowVibroSwitch", al),
				masterTextColor);
			allowVibro = (Switch)solutionPage.FindByName ("AllowVibro");
			allowVibro.IsToggled = NotificationsSupport.AllowVibro;
			allowVibro.Toggled += AllowVibro_Toggled;
			allowVibro.IsVisible = AllowNotificationSettings;

			if (AllowNotificationSettings)
				ApplyLabelSettings (solutionPage, "AllowOnLockedScreenLabel", Localization.GetText ("AllowOnLockedScreenSwitch", al),
				masterTextColor);
			allowOnLockedScreen = (Switch)solutionPage.FindByName ("AllowOnLockedScreen");
			allowOnLockedScreen.IsToggled = NotificationsSupport.AllowOnLockedScreen;
			allowOnLockedScreen.Toggled += AllowOnLockedScreen_Toggled;
			allowOnLockedScreen.IsVisible = AllowNotificationSettings;

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
			ApplyButtonSettings (solutionPage, "OccIncButton", "+", solutionFieldBackColor, OccurrenceChanged);
			ApplyButtonSettings (solutionPage, "OccDecButton", "–", solutionFieldBackColor, OccurrenceChanged);
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
			ApplyLabelSettings (aboutPage, "LanguageLabel", Localization.GetText ("LanguageLabel", al), masterTextColor);

			#endregion

			#region Страница лога приложения

			mainLog = (Editor)logPage.FindByName ("MainLog");

			mainLog.AutoSize = EditorAutoSizeOption.TextChanges;
			mainLog.FontAttributes = FontAttributes.None;
			mainLog.FontFamily = "Serif";
			mainLog.Keyboard = Keyboard.Default;
			mainLog.MaxLength = (int)ProgramDescription.MasterLogMaxLength;
			//childEditor.Placeholder = "...";
			//childEditor.PlaceholderColor = Color.FromRgb (255, 255, 0);
			mainLog.Margin = margin;
			mainLog.Text = NotificationsSupport.MasterLog;
			mainLog.IsReadOnly = true;

			notificationButton = ApplyButtonSettings (logPage, "NotificationButton",
				Localization.GetText ("NotificationButton", al), logFieldBackColor, SelectLogNotification);

			shareButton = ApplyButtonSettings (logPage, "ShareButton", Localization.GetText ("ShareButton", al),
				logFieldBackColor, ShareText);
			shareOffsetButton = ApplyButtonSettings (logPage, "ShareOffsetButton", "↓", logFieldBackColor,
				ShareOffsetButton_Clicked);
			shareOffsetLabel = ApplyLabelSettings (logPage, "ShareOffsetLabel", "", masterTextColor);

			shareButton.Margin = shareOffsetButton.Margin = shareOffsetLabel.Margin = new Thickness (1);
			ShareOffsetButton_Clicked (null, null);

			getGMJButton = ApplyButtonSettings (logPage, "GetGMJ", "GMJ", logFieldBackColor, GetGMJ);
			getGMJButton.IsVisible = (al == SupportedLanguages.ru_ru);

			// Настройки, связанные с журналом
			ApplyLabelSettings (solutionPage, "LogSettingsLabel", Localization.GetText ("LogSettingsLabel", al), masterHeaderColor);

			ApplyLabelSettings (solutionPage, "ReadModeLabel", Localization.GetText ("ReadModeLabel", al), masterTextColor);
			readModeSwitch = (Switch)solutionPage.FindByName ("ReadModeSwitch");
			readModeSwitch.IsToggled = NotificationsSupport.LogReadingMode;
			readModeSwitch.Toggled += ReadModeSwitch_Toggled;
			ReadModeSwitch_Toggled (null, null);

			//ApplyLabelSettings (solutionPage, "FontSizeLabel", Localization.GetText ("FontSizeLabel", al), masterTextColor);
			fontSizeFieldLabel = ApplyLabelSettings (solutionPage, "FontSizeFieldLabel", "", masterTextColor);
			ApplyButtonSettings (solutionPage, "FontSizeIncButton", "+", solutionFieldBackColor, FontSizeChanged);
			ApplyButtonSettings (solutionPage, "FontSizeDecButton", "–", solutionFieldBackColor, FontSizeChanged);
			FontSizeChanged (null, null);

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
				Localization.GetText ("Tip02", al), Localization.GetText ("NextButton", al));

			await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
				Localization.GetText ("Tip04", al), Localization.GetText ("NextButton", al));
			}

		// Страница проекта
		private void AppButton_Clicked (object sender, EventArgs e)
			{
			Launcher.OpenAsync ("https://github.com/adslbarxatov/" + ProgramDescription.AssemblyMainName);
			}

		// Страница видеоруководства
		private void ManualButton_Clicked (object sender, EventArgs e)
			{
			Launcher.OpenAsync (ProgramDescription.AboutLink);
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
				currentOcc = ns.Notifications[i].OccurrenceNumber;
				OccurrenceChanged (null, null);
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

		// Запрос записи из GMJ
		private async void GetGMJ (object sender, EventArgs e)
			{
			getGMJButton.IsEnabled = false;

			string s = Notification.GetRandomGMJ ();
			if (s == "")
				await logPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					"GMJ не вернула сообщение. Проверьте интернет-соединение", "ОК");
			else
				mainLog.Text = NotificationsSupport.MasterLog = s + "\r\n\r\n\r\n" + NotificationsSupport.MasterLog;

			getGMJButton.IsEnabled = true;
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
		private void OccurrenceChanged (object sender, EventArgs e)
			{
			if (sender != null)
				{
				Button b = (Button)sender;
				if ((b.Text == "+") && (currentOcc < Notification.MaxOccurrenceNumber))
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
			currentOcc = ns.NotificationsTemplates.GetOccurrenceNumber (templateNumber);
			OccurrenceChanged (null, null);
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
			beginningField.Text = beginning;
			endingField.Text = ending;
			}

		// Метод запускает интерфейс "Поделиться" для последней записи в журнале
		private async void ShareText (object sender, EventArgs e)
			{
			// Контроль
			if (!NotificationsSupport.ShareDisclaimerShown)
				{
				await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					Localization.GetText ("ShareDisclaimer", al), Localization.GetText ("NextButton", al));

				NotificationsSupport.ShareDisclaimerShown = true;
				}

			// Получение текста
			string text = NotificationsSupport.MasterLog;
			int start = 0, end = -6, i;
			for (i = 0; i < shareOffset; i++)
				{
				start = end + 6;
				end = text.IndexOf ("\r\n\r\n\r\n", start);
				if (end < 0)
					return;
				}
			text = text.Substring (start, end - start);

			// Получение ссылки
			for (i = 0; i < ns.Notifications.Count; i++)
				{
				if (text.Contains (ns.Notifications[i].Name))
					{
					text += ("\r\n\r\n" + ns.Notifications[i].Link);
					break;
					}
				}

			// Запуск
			NotificationsSupport.SkipNextServiceStart ();
			await Share.RequestAsync (new ShareTextRequest
				{
				Text = text,
				Title = ProgramDescription.AssemblyTitle
				});

			// Сброс смещения
			shareOffset = 0;
			ShareOffsetButton_Clicked (null, null);
			}

		// Включение / выключение светодиода
		private void ReadModeSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			if (e != null)
				NotificationsSupport.LogReadingMode = readModeSwitch.IsToggled;

			if (readModeSwitch.IsToggled)
				{
				logPage.BackgroundColor = mainLog.BackgroundColor = masterHeaderColor;
				notificationButton.TextColor = shareButton.TextColor = getGMJButton.TextColor =
					mainLog.TextColor = shareOffsetButton.TextColor = shareOffsetLabel.TextColor = logMasterBackColor;
				}
			else
				{
				logPage.BackgroundColor = mainLog.BackgroundColor = logMasterBackColor;
				notificationButton.TextColor = shareButton.TextColor = getGMJButton.TextColor =
					mainLog.TextColor = shareOffsetButton.TextColor = shareOffsetLabel.TextColor = masterHeaderColor;
				}
			}

		// Изменение значения частоты опроса
		private void FontSizeChanged (object sender, EventArgs e)
			{
			if (e != null)
				{
				Button b = (Button)sender;
				if ((b.Text == "+") && (mainLog.FontSize < 30))
					mainLog.FontSize += 1;
				else if ((b.Text == "–") && (mainLog.FontSize > 10))
					mainLog.FontSize -= 1;

				NotificationsSupport.LogFontSize = (uint)mainLog.FontSize;
				}
			else
				{
				mainLog.FontSize = NotificationsSupport.LogFontSize;
				}

			fontSizeFieldLabel.Text = Localization.GetText ("FontSizeLabel", al) + mainLog.FontSize.ToString ();
			}

		// Изменение смещения функции share
		private uint shareOffset = 0;
		private void ShareOffsetButton_Clicked (object sender, EventArgs e)
			{
			if (shareOffset < 5)
				shareOffset++;
			else
				shareOffset = 1;

			shareOffsetLabel.Text = " " + shareOffset.ToString ();
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
