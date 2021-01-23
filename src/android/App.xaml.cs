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

		private SupportedLanguages al = Localization.CurrentLanguage;
		private NotificationsSet ns = new NotificationsSet (false);
		private int currentNotification = 0;

		private readonly Color
			logMasterBackColor = Color.FromHex ("#F0F0F0"),
			logFieldBackColor = Color.FromHex ("#80808080"),
			logReadModeColor = Color.FromHex ("#202020"),

			solutionMasterBackColor = Color.FromHex ("#F0F8FF"),
			solutionFieldBackColor = Color.FromHex ("#D0E8FF"),

			aboutMasterBackColor = Color.FromHex ("#F0FFF0"),
			aboutFieldBackColor = Color.FromHex ("#D0FFD0");

		#endregion

		#region Переменные страниц

		private ContentPage solutionPage, aboutPage, logPage;
		private Label aboutLabel, freqFieldLabel, occFieldLabel, fontSizeFieldLabel, shareOffsetLabel;
		private Switch allowStart, allowSound, allowLight, allowVibro, allowOnLockedScreen,
			enabledSwitch, readModeSwitch, notCompleteReset, specialNotifications;
		private Button selectedNotification, applyButton, addButton, deleteButton, getGMJButton,
			shareButton, shareOffsetButton, notificationButton;
		private Editor nameField, linkField, beginningField, endingField, mainLog;
		private uint currentOcc, currentFreq;

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

			solutionPage = AndroidSupport.ApplyPageSettings (MainPage, "SolutionPage",
				Localization.GetText ("SolutionPage", al), solutionMasterBackColor);
			aboutPage = AndroidSupport.ApplyPageSettings (MainPage, "AboutPage",
				Localization.GetText ("AboutPage", al), aboutMasterBackColor);
			logPage = AndroidSupport.ApplyPageSettings (MainPage, "LogPage",
				Localization.GetText ("LogPage", al), logMasterBackColor);

			if (CurrentTabNumber > 1)
				CurrentTabNumber = 1;
			((CarouselPage)MainPage).CurrentPage = ((CarouselPage)MainPage).Children[(int)CurrentTabNumber];

			#region Настройки службы

			AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "ServiceSettingsLabel",
				Localization.GetText ("ServiceSettingsLabel", al), true);

			AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "AllowStartLabel",
				Localization.GetText ("AllowStartSwitch", al), false);
			allowStart = (Switch)solutionPage.FindByName ("AllowStart");
			allowStart.IsToggled = NotificationsSupport.AllowServiceToStart;
			allowStart.Toggled += AllowStart_Toggled;

			if (AllowNotificationSettings)
				AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "AlarmSettingsLabel",
					Localization.GetText ("AlarmSettingsLabel", al), true);

			if (AllowNotificationSettings)
				AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "AllowSoundLabel",
					Localization.GetText ("AllowSoundSwitch", al), false);
			allowSound = (Switch)solutionPage.FindByName ("AllowSound");
			allowSound.IsToggled = NotificationsSupport.AllowSound;
			allowSound.Toggled += AllowSound_Toggled;
			allowSound.IsVisible = AllowNotificationSettings;

			if (AllowNotificationSettings)
				AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "AllowLightLabel",
					Localization.GetText ("AllowLightSwitch", al), false);
			allowLight = (Switch)solutionPage.FindByName ("AllowLight");
			allowLight.IsToggled = NotificationsSupport.AllowLight;
			allowLight.Toggled += AllowLight_Toggled;
			allowLight.IsVisible = AllowNotificationSettings;

			if (AllowNotificationSettings)
				AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "AllowVibroLabel",
					Localization.GetText ("AllowVibroSwitch", al), false);
			allowVibro = (Switch)solutionPage.FindByName ("AllowVibro");
			allowVibro.IsToggled = NotificationsSupport.AllowVibro;
			allowVibro.Toggled += AllowVibro_Toggled;
			allowVibro.IsVisible = AllowNotificationSettings;

			if (AllowNotificationSettings)
				AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "AllowOnLockedScreenLabel",
					Localization.GetText ("AllowOnLockedScreenSwitch", al), false);
			allowOnLockedScreen = (Switch)solutionPage.FindByName ("AllowOnLockedScreen");
			allowOnLockedScreen.IsToggled = NotificationsSupport.AllowOnLockedScreen;
			allowOnLockedScreen.Toggled += AllowOnLockedScreen_Toggled;
			allowOnLockedScreen.IsVisible = AllowNotificationSettings;

			if (al == SupportedLanguages.ru_ru)
				AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "SpecialNotificationsLabel",
					Localization.GetText ("SpecialNotificationsSwitch", al), false);
			specialNotifications = (Switch)solutionPage.FindByName ("SpecialNotifications");
			specialNotifications.IsToggled = ns.AddSpecialNotifications;
			specialNotifications.Toggled += AddSpecialNotifications_Toggled;
			if (al != SupportedLanguages.ru_ru)
				specialNotifications.IsVisible = specialNotifications.IsToggled = false;

			AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "CompleteResetLabel",
				Localization.GetText ("CompleteResetSwitch", al), false);
			notCompleteReset = (Switch)solutionPage.FindByName ("CompleteReset");
			notCompleteReset.IsToggled = NotificationsSupport.NotCompleteReset;
			notCompleteReset.Toggled += CompleteReset_Toggled;

			#endregion

			#region Настройки оповещений

			AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "NotificationsSettingsLabel",
				Localization.GetText ("NotificationsSettingsLabel", al), true);
			selectedNotification = AndroidSupport.ApplyButtonSettings (solutionPage, "SelectedNotification",
				"", solutionFieldBackColor, SelectNotification);

			AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "NameFieldLabel",
				Localization.GetText ("NameFieldLabel", al), false);
			nameField = AndroidSupport.ApplyEditorSettings (solutionPage, "NameField", solutionFieldBackColor,
				Keyboard.Default, Notification.MaxBeginningEndingLength, "", null);

			AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "LinkFieldLabel",
				Localization.GetText ("LinkFieldLabel", al), false);
			linkField = AndroidSupport.ApplyEditorSettings (solutionPage, "LinkField", solutionFieldBackColor,
				Keyboard.Url, 150, "", null);

			AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "BeginningFieldLabel",
				Localization.GetText ("BeginningFieldLabel", al), false);
			beginningField = AndroidSupport.ApplyEditorSettings (solutionPage, "BeginningField", solutionFieldBackColor,
				Keyboard.Url, Notification.MaxBeginningEndingLength, "", null);

			AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "EndingFieldLabel",
				Localization.GetText ("EndingFieldLabel", al), false);
			endingField = AndroidSupport.ApplyEditorSettings (solutionPage, "EndingField", solutionFieldBackColor,
				Keyboard.Url, Notification.MaxBeginningEndingLength, "", null);

			freqFieldLabel = AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "FreqFieldLabel", "", false);
			AndroidSupport.ApplyButtonSettings (solutionPage, "FreqIncButton", "+", solutionFieldBackColor, FrequencyChanged);
			AndroidSupport.ApplyButtonSettings (solutionPage, "FreqDecButton", "–", solutionFieldBackColor, FrequencyChanged);
			currentFreq = 1;

			occFieldLabel = AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "OccFieldLabel", "", false);
			AndroidSupport.ApplyButtonSettings (solutionPage, "OccIncButton", "+", solutionFieldBackColor, OccurrenceChanged);
			AndroidSupport.ApplyButtonSettings (solutionPage, "OccDecButton", "–", solutionFieldBackColor, OccurrenceChanged);
			currentOcc = 1;

			enabledSwitch = (Switch)solutionPage.FindByName ("EnabledSwitch");

			// Инициализация полей
			SelectNotification (null, null);

			#endregion

			#region Управление оповещениями

			applyButton = AndroidSupport.ApplyButtonSettings (solutionPage, "ApplyButton",
				Localization.GetText ("ApplyButton", al), solutionFieldBackColor, ApplyNotification);
			addButton = AndroidSupport.ApplyButtonSettings (solutionPage, "AddButton",
				Localization.GetText ("AddButton", al), solutionFieldBackColor, AddNotification);
			deleteButton = AndroidSupport.ApplyButtonSettings (solutionPage, "DeleteButton",
				Localization.GetText ("DeleteButton", al), solutionFieldBackColor, DeleteNotification);

			AndroidSupport.ApplyButtonSettings (solutionPage, "TemplateButton",
				Localization.GetText ("TemplateButton", al), solutionFieldBackColor, LoadTemplate);
			AndroidSupport.ApplyButtonSettings (solutionPage, "FindDelimitersButton",
				Localization.GetText ("FindDelimitersButton", al), solutionFieldBackColor, FindDelimiters);

			#endregion

			#region Страница "О программе"

			aboutLabel = AndroidSupport.ApplyLabelSettings (aboutPage, "AboutLabel",
				ProgramDescription.AssemblyTitle + "\n" +
				ProgramDescription.AssemblyDescription + "\n\n" +
				ProgramDescription.AssemblyCopyright + "\nv " +
				ProgramDescription.AssemblyVersion +
				"; " + ProgramDescription.AssemblyLastUpdate,
				Color.FromHex ("#000080"));
			aboutLabel.FontAttributes = FontAttributes.Bold;
			aboutLabel.HorizontalOptions = LayoutOptions.Fill;
			aboutLabel.HorizontalTextAlignment = TextAlignment.Center;

			AndroidSupport.ApplyButtonSettings (aboutPage, "AppPage", Localization.GetText ("AppPage", al),
				aboutFieldBackColor, AppButton_Clicked);
			AndroidSupport.ApplyButtonSettings (aboutPage, "ManualPage", Localization.GetText ("ManualPage", al),
				aboutFieldBackColor, ManualButton_Clicked);
			AndroidSupport.ApplyButtonSettings (aboutPage, "ADPPage", Localization.GetText ("ADPPage", al),
				aboutFieldBackColor, ADPButton_Clicked);
			AndroidSupport.ApplyButtonSettings (aboutPage, "DevPage", Localization.GetText ("DevPage", al),
				aboutFieldBackColor, DevButton_Clicked);
			AndroidSupport.ApplyButtonSettings (aboutPage, "CommunityPage",
				"RD AAOW Free utilities production lab", aboutFieldBackColor, CommunityButton_Clicked);

			UpdateButtons ();

			AndroidSupport.ApplyButtonSettings (aboutPage, "LanguageSelector", Localization.LanguagesNames[(int)al],
				aboutFieldBackColor, SelectLanguage_Clicked);
			AndroidSupport.ApplyLabelSettingsForKKT (aboutPage, "LanguageLabel",
				Localization.GetText ("LanguageLabel", al), false);

			#endregion

			#region Страница лога приложения

			mainLog = AndroidSupport.ApplyEditorSettings (logPage, "MainLog", logFieldBackColor, Keyboard.Default,
				ProgramDescription.MasterLogMaxLength, NotificationsSupport.MasterLog, null);
			mainLog.IsReadOnly = true;

			notificationButton = AndroidSupport.ApplyButtonSettings (logPage, "NotificationButton",
				Localization.GetText ("NotificationButton", al), logFieldBackColor, SelectLogNotification);

			shareButton = AndroidSupport.ApplyButtonSettings (logPage, "ShareButton", Localization.GetText ("ShareButton", al),
				logFieldBackColor, ShareText);
			shareOffsetButton = AndroidSupport.ApplyButtonSettings (logPage, "ShareOffsetButton", "▼", logFieldBackColor,
				ShareOffsetButton_Clicked);
			shareOffsetLabel = AndroidSupport.ApplyLabelSettingsForKKT (logPage, "ShareOffsetLabel", "", false);

			shareButton.Margin = shareOffsetButton.Margin = shareOffsetLabel.Margin = new Thickness (1);
			ShareOffsetButton_Clicked (null, null);

			getGMJButton = AndroidSupport.ApplyButtonSettings (logPage, "GetGMJ", "GMJ", logFieldBackColor, GetGMJ);
			getGMJButton.IsVisible = (al == SupportedLanguages.ru_ru);

			// Настройки, связанные с журналом
			AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "LogSettingsLabel",
				Localization.GetText ("LogSettingsLabel", al), true);

			AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "ReadModeLabel",
				Localization.GetText ("ReadModeLabel", al), false);
			readModeSwitch = (Switch)solutionPage.FindByName ("ReadModeSwitch");
			readModeSwitch.IsToggled = NotificationsSupport.LogReadingMode;
			readModeSwitch.Toggled += ReadModeSwitch_Toggled;
			ReadModeSwitch_Toggled (null, null);

			fontSizeFieldLabel = AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "FontSizeFieldLabel", "", false);
			AndroidSupport.ApplyButtonSettings (solutionPage, "FontSizeIncButton", "+", solutionFieldBackColor, FontSizeChanged);
			AndroidSupport.ApplyButtonSettings (solutionPage, "FontSizeDecButton", "–", solutionFieldBackColor, FontSizeChanged);
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

		// Включение / выключение специальных оповещений
		private void AddSpecialNotifications_Toggled (object sender, ToggledEventArgs e)
			{
			ns.AddSpecialNotifications = specialNotifications.IsToggled;
			}

		// Включение / выключение полного сброса при вызове функции Опросить все
		private void CompleteReset_Toggled (object sender, ToggledEventArgs e)
			{
			NotificationsSupport.NotCompleteReset = notCompleteReset.IsToggled;
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
		private async void CommunityButton_Clicked (object sender, EventArgs e)
			{
			if ((al == SupportedLanguages.ru_ru) && await aboutPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					"", "ВКонтакте", "Телеграм") ||
				await aboutPage.DisplayAlert (ProgramDescription.AssemblyTitle, "", "Main community", "Telegram feed"))
				{
				Launcher.OpenAsync ("https://vk.com/@rdaaow_fupl-user-manuals");
				}
			else
				{
				Launcher.OpenAsync ("https://t.me/rdaaow_fupl");
				}
			}

		// Страница политики и EULA
		private void ADPButton_Clicked (object sender, EventArgs e)
			{
			Launcher.OpenAsync ("https://vk.com/@rdaaow_fupl-adp");
			}

		// Страница политики и EULA
		private async void DevButton_Clicked (object sender, EventArgs e)
			{
			try
				{
				EmailMessage message = new EmailMessage
					{
					Subject = "Wish, advice or bug in " + ProgramDescription.AssemblyTitle,
					Body = "",
					To = new List<string> () { "adslbarxatov@gmail.com" }
					//Cc = ccRecipients,
					//Bcc = bccRecipients
					};
				await Email.ComposeAsync (message);
				}
			catch
				{
				await aboutPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					Localization.GetText ("EmailsAreUnavailable", al),
					Localization.GetText ("NextButton", al));
				}
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
				logPage.BackgroundColor = mainLog.BackgroundColor = logReadModeColor;
				notificationButton.TextColor = shareButton.TextColor = getGMJButton.TextColor =
					mainLog.TextColor = shareOffsetButton.TextColor = shareOffsetLabel.TextColor = logMasterBackColor;
				}
			else
				{
				logPage.BackgroundColor = mainLog.BackgroundColor = logMasterBackColor;
				notificationButton.TextColor = shareButton.TextColor = getGMJButton.TextColor =
					mainLog.TextColor = shareOffsetButton.TextColor = shareOffsetLabel.TextColor = logReadModeColor;
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
					n.ResetTimer (true);
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
