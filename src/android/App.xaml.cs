using Android.Widget;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
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
		private CultureInfo ci;
		private DateTime lastNotStamp = new DateTime (2000, 1, 1, 0, 0, 0);

		private int currentNotification = 0;

		private readonly Color
			logMasterBackColor = Color.FromHex ("#F0F0F0"),
			logFieldBackColor = Color.FromHex ("#80808080"),
			logReadModeColor = Color.FromHex ("#202020"),

			solutionMasterBackColor = Color.FromHex ("#FFF8F0"),
			solutionFieldBackColor = Color.FromHex ("#FFE8D0"),

			aboutMasterBackColor = Color.FromHex ("#F0FFF0"),
			aboutFieldBackColor = Color.FromHex ("#D0FFD0");

		#endregion

		#region Переменные страниц

		private ContentPage solutionPage, aboutPage, logPage;
		private Label aboutLabel, occFieldLabel, fontSizeFieldLabel;
		private Xamarin.Forms.Switch allowStart, enabledSwitch, readModeSwitch, specialNotifications;
		private Xamarin.Forms.Button selectedNotification, applyButton, addButton, deleteButton, getGMJButton,
			shareButton, shareOffsetButton, notificationButton, nextNewsButton, allNewsButton;
		private Editor nameField, linkField, beginningField, endingField, mainLog;
		private uint currentOcc;

		#endregion

		/// <summary>
		/// Конструктор. Точка входа приложения
		/// </summary>
		/// <param name="CurrentTabNumber">Номер текущей вкладки при старте</param>
		/// <param name="AllowNotificationSettings">Флаг, указывающий на возможность настройки оповещений из приложения</param>
		/// <param name="AllowForeground">Флаг, указывающий на возможность работы в ждущем режиме</param>
		public App (uint CurrentTabNumber, bool AllowNotificationSettings, bool AllowForeground)
			{
			// Инициализация
			InitializeComponent ();
			if (ProgramDescription.NSet == null)
				ProgramDescription.NSet = new NotificationsSet (false);

			// Сброс частоты обновления
			if (!NotificationsSupport.FrequencyLock)
				{
				for (int i = 0; i < ProgramDescription.NSet.Notifications.Count; i++)
					ProgramDescription.NSet.Notifications[i] =
						new Notification (ProgramDescription.NSet.Notifications[i].Name,
						ProgramDescription.NSet.Notifications[i].Link,
						ProgramDescription.NSet.Notifications[i].Beginning,
						ProgramDescription.NSet.Notifications[i].Ending, 1,
						ProgramDescription.NSet.Notifications[i].OccurrenceNumber);

				ProgramDescription.NSet.SaveNotifications ();
				NotificationsSupport.FrequencyLock = true;
				}

			// Инициализация представления даты и времени 
			try
				{
				if (al == SupportedLanguages.ru_ru)
					ci = new CultureInfo ("ru-ru");
				else
					ci = new CultureInfo ("en-us");
				}
			catch
				{
				ci = CultureInfo.InstalledUICulture;
				}

			// Переход в статус запуска для отмены вызова из оповещения
			NotificationsSupport.AppIsRunning = true;

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
			allowStart = (Xamarin.Forms.Switch)solutionPage.FindByName ("AllowStart");
			allowStart.IsToggled = NotificationsSupport.AllowServiceToStart;
			allowStart.Toggled += AllowStart_Toggled;

			if (al == SupportedLanguages.ru_ru)
				AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "SpecialNotificationsLabel",
					Localization.GetText ("SpecialNotificationsSwitch", al), false);
			specialNotifications = (Xamarin.Forms.Switch)solutionPage.FindByName ("SpecialNotifications");
			if (al == SupportedLanguages.ru_ru)
				{
				specialNotifications.IsToggled = ProgramDescription.NSet.AddSpecialNotifications;
				specialNotifications.Toggled += AddSpecialNotifications_Toggled;
				}
			else
				{
				specialNotifications.IsVisible = specialNotifications.IsToggled = false;
				}

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
			nameField.Placeholder = Localization.GetText ("NameFieldPlaceholder", al);

			linkField = AndroidSupport.ApplyEditorSettings (solutionPage, "LinkField", solutionFieldBackColor,
				Keyboard.Url, 150, "", null);
			linkField.Placeholder = Localization.GetText ("LinkFieldPlaceholder", al);

			AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "BeginningFieldLabel",
				Localization.GetText ("BeginningFieldLabel", al), false);
			beginningField = AndroidSupport.ApplyEditorSettings (solutionPage, "BeginningField", solutionFieldBackColor,
				Keyboard.Url, Notification.MaxBeginningEndingLength, "", null);

			AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "EndingFieldLabel",
				Localization.GetText ("EndingFieldLabel", al), false);
			endingField = AndroidSupport.ApplyEditorSettings (solutionPage, "EndingField", solutionFieldBackColor,
				Keyboard.Url, Notification.MaxBeginningEndingLength, "", null);

			occFieldLabel = AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "OccFieldLabel", "", false);
			AndroidSupport.ApplyButtonSettings (solutionPage, "OccIncButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Increase),
				solutionFieldBackColor, OccurrenceChanged);
			AndroidSupport.ApplyButtonSettings (solutionPage, "OccDecButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Decrease),
				solutionFieldBackColor, OccurrenceChanged);
			currentOcc = 1;

			AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "EnabledLabel",
				Localization.GetText ("EnabledLabel", al), false);
			enabledSwitch = (Xamarin.Forms.Switch)solutionPage.FindByName ("EnabledSwitch");

			// Инициализация полей
			SelectNotification (null, null);

			#endregion

			#region Управление оповещениями

			applyButton = AndroidSupport.ApplyButtonSettings (solutionPage, "ApplyButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Apply),
				solutionFieldBackColor, ApplyNotification);
			addButton = AndroidSupport.ApplyButtonSettings (solutionPage, "AddButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Create),
				solutionFieldBackColor, AddNotification);
			deleteButton = AndroidSupport.ApplyButtonSettings (solutionPage, "DeleteButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Delete),
				solutionFieldBackColor, DeleteNotification);

			AndroidSupport.ApplyButtonSettings (solutionPage, "ShareTemplateButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Share),
				solutionFieldBackColor, ShareTemplate);
			AndroidSupport.ApplyButtonSettings (solutionPage, "LoadTemplateButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Copy),
				solutionFieldBackColor, LoadTemplate);
			AndroidSupport.ApplyButtonSettings (solutionPage, "FindDelimitersButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Find),
				solutionFieldBackColor, FindDelimiters);

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
				AndroidSupport.MasterLabName, aboutFieldBackColor, CommunityButton_Clicked);
			AndroidSupport.ApplyButtonSettings (aboutPage, "ResetTips", Localization.GetText ("ResetTips", al),
				aboutFieldBackColor, ResetTips_Clicked);

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
			mainLog.Placeholder = Localization.GetText ("MainLogPlaceholder", al);

			notificationButton = AndroidSupport.ApplyButtonSettings (logPage, "NotificationButton",
				Localization.GetText ("NotificationButton", al), logFieldBackColor, SelectLogNotification);
			notificationButton.Margin = new Thickness (0);

			shareButton = AndroidSupport.ApplyButtonSettings (logPage, "ShareButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Share),
				logFieldBackColor, ShareText);
			shareOffsetButton = AndroidSupport.ApplyButtonSettings (logPage, "ShareOffsetButton", "", logFieldBackColor,
				ShareOffsetButton_Clicked);
			shareOffsetButton.WidthRequest = shareButton.WidthRequest;
			shareOffsetButton.Margin = shareButton.Margin;

			ShareOffsetButton_Clicked (null, null);

			nextNewsButton = AndroidSupport.ApplyButtonSettings (logPage, "NextNewsButton",
				Localization.GetText ("NextNewsButton", al), logFieldBackColor, NextNewsItem);
			nextNewsButton.Margin = new Thickness (0);

			allNewsButton = AndroidSupport.ApplyButtonSettings (logPage, "AllNewsButton",
				Localization.GetText ("AllNewsButton", al), logFieldBackColor, AllNewsItems);
			allNewsButton.Margin = new Thickness (0);

			getGMJButton = AndroidSupport.ApplyButtonSettings (logPage, "GetGMJ",
				Localization.GetText ("GMJButton", al), logFieldBackColor, GetGMJ);
			getGMJButton.Margin = new Thickness (0);
			getGMJButton.IsVisible = (al == SupportedLanguages.ru_ru);

			// Настройки, связанные с журналом
			AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "LogSettingsLabel",
				Localization.GetText ("LogSettingsLabel", al), true);

			AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "ReadModeLabel",
				Localization.GetText ("ReadModeLabel", al), false);
			readModeSwitch = (Xamarin.Forms.Switch)solutionPage.FindByName ("ReadModeSwitch");
			readModeSwitch.IsToggled = NotificationsSupport.LogReadingMode;
			readModeSwitch.Toggled += ReadModeSwitch_Toggled;
			ReadModeSwitch_Toggled (null, null);

			fontSizeFieldLabel = AndroidSupport.ApplyLabelSettingsForKKT (solutionPage, "FontSizeFieldLabel", "", false);
			AndroidSupport.ApplyButtonSettings (solutionPage, "FontSizeIncButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Increase),
				solutionFieldBackColor, FontSizeChanged);
			AndroidSupport.ApplyButtonSettings (solutionPage, "FontSizeDecButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Decrease),
				solutionFieldBackColor, FontSizeChanged);
			FontSizeChanged (null, null);

			#endregion

			// Принятие соглашений
			ShowStartupTips (AllowNotificationSettings, AllowForeground);
			}

		// Включение / выключение службы
		private void AllowStart_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.ServiceLaunchTip))
				ShowTips (NotificationsSupport.TipTypes.ServiceLaunchTip, solutionPage);

			NotificationsSupport.AllowServiceToStart = allowStart.IsToggled;
			}

		// Включение / выключение специальных оповещений
		private void AddSpecialNotifications_Toggled (object sender, ToggledEventArgs e)
			{
			ProgramDescription.NSet.AddSpecialNotifications = specialNotifications.IsToggled;
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
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RestartApp", al),
					ToastLength.Long).Show ();
				}
			}

		// Метод отображает подсказки при первом запуске
		private async void ShowStartupTips (bool AllowNotSettings, bool AllowForeground)
			{
			// Требование принятия Политики
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.PolicyTip))
				{
				while (await logPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					Localization.GetText ("PolicyMessage", al),
					Localization.GetText ("DeclineButton", al),
					Localization.GetText ("AcceptButton", al)))
					{
					ADPButton_Clicked (null, null);
					}
				NotificationsSupport.SetTipState (NotificationsSupport.TipTypes.PolicyTip);
				}

			// Подсказки
			if (NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.StartupTips))
				return;

			await logPage.DisplayAlert (Localization.GetText ("TipHeader01", al),
				Localization.GetText ("Tip01", al), Localization.GetText ("NextButton", al));

			await logPage.DisplayAlert (Localization.GetText ("TipHeader01", al),
				Localization.GetText ("Tip02", al), Localization.GetText ("NextButton", al));

			await logPage.DisplayAlert (Localization.GetText ("TipHeader01", al),
				Localization.GetText ("Tip03", al), Localization.GetText ("NextButton", al));

			NotificationsSupport.SetTipState (NotificationsSupport.TipTypes.StartupTips);
			}

		// Метод отображает остальные подсказки
		private async void ShowTips (NotificationsSupport.TipTypes Type, Page DisplayPage)
			{
			// Подсказки
			await DisplayPage.DisplayAlert (Localization.GetText ("TipHeader01", al) /*+ ((int)Type + 3).ToString ()*/,
				Localization.GetText ("Tip04_" + ((int)Type).ToString (), al), Localization.GetText ("NextButton", al));

			NotificationsSupport.SetTipState (Type);
			}

		// Страница проекта
		private async void AppButton_Clicked (object sender, EventArgs e)
			{
			try
				{
				await Launcher.OpenAsync (AndroidSupport.MasterGitLink + ProgramDescription.AssemblyMainName);
				}
			catch
				{
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("WebIsUnavailable", al),
					ToastLength.Long).Show ();
				}
			}

		// Страница видеоруководства
		private async void ManualButton_Clicked (object sender, EventArgs e)
			{
			try
				{
				await Launcher.OpenAsync (ProgramDescription.AboutLink);
				}
			catch
				{
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("WebIsUnavailable", al),
					ToastLength.Long).Show ();
				}
			}

		// Сброс подсказок
		private void ResetTips_Clicked (object sender, EventArgs e)
			{
			NotificationsSupport.SetTipState ();
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RestartApp", al),
				ToastLength.Long).Show ();
			}

		// Страница лаборатории
		private async void CommunityButton_Clicked (object sender, EventArgs e)
			{
			try
				{
				if (await aboutPage.DisplayAlert (ProgramDescription.AssemblyTitle,
						Localization.GetText ("CommunitySelect", al), Localization.GetText ("CommunityVK", al),
						Localization.GetText ("CommunityTG", al)))
					await Launcher.OpenAsync (AndroidSupport.CommunityFrontPage);
				else
					await Launcher.OpenAsync (AndroidSupport.CommunityInTelegram);
				}
			catch
				{
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("WebIsUnavailable", al),
					ToastLength.Long).Show ();
				}
			}

		// Страница политики и EULA
		private async void ADPButton_Clicked (object sender, EventArgs e)
			{
			try
				{
				await Launcher.OpenAsync (AndroidSupport.ADPLink);
				}
			catch
				{
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("WebIsUnavailable", al),
					ToastLength.Long).Show ();
				}
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
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("EmailsAreUnavailable", al),
					ToastLength.Long).Show ();
				}
			}

		// Выбор оповещения
		private async void SelectNotification (object sender, EventArgs e)
			{
			// Подсказки
			if ((e != null) && !NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.CurrentNotButton))
				{
				ShowTips (NotificationsSupport.TipTypes.CurrentNotButton, solutionPage);
				return;
				}

			// Запрос списка оповещений
			List<string> list = new List<string> ();
			foreach (Notification element in ProgramDescription.NSet.Notifications)
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

				nameField.Text = ProgramDescription.NSet.Notifications[i].Name;
				linkField.Text = ProgramDescription.NSet.Notifications[i].Link;
				beginningField.Text = ProgramDescription.NSet.Notifications[i].Beginning;
				endingField.Text = ProgramDescription.NSet.Notifications[i].Ending;
				currentOcc = ProgramDescription.NSet.Notifications[i].OccurrenceNumber;
				OccurrenceChanged (null, null);
				enabledSwitch.IsToggled = ProgramDescription.NSet.Notifications[i].IsEnabled;
				}

			// Сброс
			list.Clear ();
			}

		// Выбор оповещения для перехода по ссылке
		private async void SelectLogNotification (object sender, EventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.GoToButton))
				{
				ShowTips (NotificationsSupport.TipTypes.GoToButton, logPage);
				return;
				}

			// Список ресурсов
			List<string> list = new List<string> ();
			foreach (Notification element in ProgramDescription.NSet.Notifications)
				list.Add (element.Name);

			string res = await logPage.DisplayActionSheet (Localization.GetText ("SelectNotification", al),
				Localization.GetText ("CancelButton", al), null, list.ToArray ());

			// Запуск
			int i;
			if ((i = list.IndexOf (res)) >= 0)
				{
				try
					{
					await Launcher.OpenAsync (ProgramDescription.NSet.Notifications[i].Link);
					}
				catch
					{
					Toast.MakeText (Android.App.Application.Context, Localization.GetText ("WebIsUnavailable", al),
						ToastLength.Long).Show ();
					}
				}

			// Сброс
			list.Clear ();
			}

		// Запрос записи из GMJ
		private async void GetGMJ (object sender, EventArgs e)
			{
			getGMJButton.IsEnabled = nextNewsButton.IsEnabled = allNewsButton.IsEnabled = notificationButton.IsEnabled =
				shareButton.IsEnabled = false;
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestStarted", al),
				ToastLength.Short).Show ();

			NotificationsSupport.StopRequested = false; // Разблокировка метода GetHTML
			string newText = await Task.Run<string> (GMJ.GetRandomGMJ);

			if (newText == "")
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("GMJRequestFailed", al),
					ToastLength.Long).Show ();
			else
				mainLog.Text = NotificationsSupport.MasterLog = newText + "\r\n\r\n\r\n" + NotificationsSupport.MasterLog;

			getGMJButton.IsEnabled = nextNewsButton.IsEnabled = allNewsButton.IsEnabled = notificationButton.IsEnabled =
				shareButton.IsEnabled = true;
			}

		// Изменение порядкового номера вхождения
		private void OccurrenceChanged (object sender, EventArgs e)
			{
			// Изменение значения
			if (sender != null)
				{
				// Подсказки
				if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.OccurenceTip))
					ShowTips (NotificationsSupport.TipTypes.OccurenceTip, solutionPage);

				Xamarin.Forms.Button b = (Xamarin.Forms.Button)sender;
				if ((b.Text == AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Increase)) &&
					(currentOcc < Notification.MaxOccurrenceNumber))
					currentOcc++;
				else if ((b.Text == AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Decrease)) &&
					(currentOcc > 1))
					currentOcc--;
				}

			occFieldLabel.Text = string.Format (Localization.GetText ("OccFieldLabel", al), currentOcc);
			}

		// Удаление оповещения
		private async void DeleteNotification (object sender, EventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.DeleteButton))
				{
				ShowTips (NotificationsSupport.TipTypes.DeleteButton, solutionPage);
				return;
				}

			// Контроль
			if (!await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
				Localization.GetText ("DeleteMessage", al), Localization.GetText ("NextButton", al),
				Localization.GetText ("CancelButton", al)))
				return;

			// Удаление и переход к другому оповещению
			ProgramDescription.NSet.Notifications.RemoveAt (currentNotification);
			if (currentNotification >= ProgramDescription.NSet.Notifications.Count)
				currentNotification = ProgramDescription.NSet.Notifications.Count - 1;
			SelectNotification (null, null);

			// Обновление контролов
			UpdateButtons ();
			}

		// Добавление нового оповещения
		private void AddNotification (object sender, EventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.AddButton))
				{
				ShowTips (NotificationsSupport.TipTypes.AddButton, solutionPage);
				return;
				}

			// Добавление
			UpdateItem (-1);

			// Выбор нового оповещения
			if (itemUpdated)
				{
				currentNotification = ProgramDescription.NSet.Notifications.Count - 1;
				SelectNotification (null, null);
				}
			}

		// Обновление оповещения
		private void ApplyNotification (object sender, EventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.ApplyButton))
				{
				ShowTips (NotificationsSupport.TipTypes.ApplyButton, solutionPage);
				return;
				}

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
				1, currentOcc);

			if (!ni.IsInited)
				{
				await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					Localization.GetText ("NotEnoughDataMessage", al), Localization.GetText ("NextButton", al));

				itemUpdated = false;
				nameField.Focus ();
				return;
				}
			if ((ItemNumber < 0) && ProgramDescription.NSet.Notifications.Contains (ni)) // Не относится к обновлению позиции
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
				ProgramDescription.NSet.Notifications.Add (ni);
				}
			else if (ItemNumber < ProgramDescription.NSet.Notifications.Count)
				{
				ProgramDescription.NSet.Notifications[ItemNumber] = ni;
				}

			// Обновление контролов
			UpdateButtons ();
			itemUpdated = true;
			}

		// Обновление кнопок
		private void UpdateButtons ()
			{
			addButton.IsVisible = (ProgramDescription.NSet.Notifications.Count < NotificationsSet.MaxNotifications);
			deleteButton.IsVisible = (ProgramDescription.NSet.Notifications.Count > 1);
			}

		// Метод загружает шаблон оповещения
		private List<string> templatesNames = new List<string> ();
		private char[] templateSplitter = new char[] { '|' };

		private async void LoadTemplate (object sender, EventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.TemplateButton))
				{
				ShowTips (NotificationsSupport.TipTypes.TemplateButton, solutionPage);
				return;
				}

			// Запрос варианта загрузки
			if (await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
				Localization.GetText ("TemplateSelect", al), Localization.GetText ("TemplateList", al),
				Localization.GetText ("TemplateClipboard", al)))
				{

				// Запрос списка шаблонов
				if (templatesNames.Count == 0)
					for (uint i = 0; i < ProgramDescription.NSet.NotificationsTemplates.TemplatesCount; i++)
						templatesNames.Add (ProgramDescription.NSet.NotificationsTemplates.GetName (i));

				string res = await solutionPage.DisplayActionSheet (Localization.GetText ("SelectTemplate", al),
					Localization.GetText ("CancelButton", al), null, templatesNames.ToArray ());

				// Установка результата
				uint templateNumber = 0;
				int r;
				if ((r = templatesNames.IndexOf (res)) >= 0)
					templateNumber = (uint)r;

				// Проверка
				if (ProgramDescription.NSet.NotificationsTemplates.IsTemplateIncomplete (templateNumber))
					await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
						Localization.GetText ("CurlyTemplate", al), Localization.GetText ("NextButton", al));

				// Заполнение
				nameField.Text = ProgramDescription.NSet.NotificationsTemplates.GetName (templateNumber);
				linkField.Text = ProgramDescription.NSet.NotificationsTemplates.GetLink (templateNumber);
				beginningField.Text = ProgramDescription.NSet.NotificationsTemplates.GetBeginning (templateNumber);
				endingField.Text = ProgramDescription.NSet.NotificationsTemplates.GetEnding (templateNumber);
				currentOcc = ProgramDescription.NSet.NotificationsTemplates.GetOccurrenceNumber (templateNumber);
				}

			// Разбор переданного шаблона
			else
				{
				// Запрос из буфера обмена
				string text = "";
				try
					{
					text = await Clipboard.GetTextAsync ();
					}
				catch
					{
					}

				if ((text == null) || (text == ""))
					{
					await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
						Localization.GetText ("NoTemplateInClipboard", al), "OK");
					return;
					}

				// Разбор
				string[] values = text.Split (templateSplitter, StringSplitOptions.RemoveEmptyEntries);
				if (values.Length != 5)
					{
					await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
						Localization.GetText ("NoTemplateInClipboard", al), "OK");
					return;
					}

				// Заполнение
				nameField.Text = values[0];
				linkField.Text = values[1];
				beginningField.Text = values[2];
				endingField.Text = values[3];
				try
					{
					currentOcc = uint.Parse (values[4]);
					}
				catch
					{
					currentOcc = 1;
					}
				}

			OccurrenceChanged (null, null);
			}

		// Метод формирует и отправляет шаблон оповещения
		private async void ShareTemplate (object sender, EventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.ShareNotButton))
				{
				ShowTips (NotificationsSupport.TipTypes.ShareNotButton, solutionPage);
				return;
				}

			// Формирование и отправка
			await Share.RequestAsync (new ShareTextRequest
				{
				Text = nameField.Text + templateSplitter[0].ToString () + linkField.Text + templateSplitter[0].ToString () +
					beginningField.Text + templateSplitter[0].ToString () + endingField.Text + templateSplitter[0].ToString () +
					currentOcc.ToString (),
				Title = ProgramDescription.AssemblyTitle
				});
			}

		// Автоматизированный поиск ограничителей
		private async void FindDelimiters (object sender, EventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.FindButton))
				{
				ShowTips (NotificationsSupport.TipTypes.FindButton, solutionPage);
				return;
				}

			// Контроль
			beginningField.Focus ();

			if ((beginningField.Text == "") || beginningField.Text.Contains ("<") || beginningField.Text.Contains (">"))
				{
				await solutionPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					Localization.GetText ("FindDisclaimer", al), Localization.GetText ("NextButton", al));
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
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.ShareButton))
				{
				ShowTips (NotificationsSupport.TipTypes.ShareButton, logPage);
				return;
				}

			// Получение текста
			string text = NotificationsSupport.MasterLog;
			int start = 0, end = -6, i;
			for (i = 0; i < shareOffset; i++)
				{
				start = end + 6;
				end = text.IndexOf ("\r\n\r\n\r\n", start);
				if (end < 0)
					{
					end = text.Length - 1;
					break;
					}
				}

			if (end - start > 0)
				text = text.Substring (start, end - start);
			else
				return;

			// Получение ссылки
			for (i = 0; i < ProgramDescription.NSet.Notifications.Count; i++)
				{
				if (text.Contains (ProgramDescription.NSet.Notifications[i].Name))
					{
					text += ("\r\n\r\n" + ProgramDescription.NSet.Notifications[i].Link);
					break;
					}
				}

			// Запуск
			await Share.RequestAsync (new ShareTextRequest
				{
				Text = text,
				Title = ProgramDescription.AssemblyTitle
				});

			// Сброс смещения
			shareOffset = 0;
			ShareOffsetButton_Clicked (null, null);
			}

		// Обновление временной метки в журнале
		private void UpdateLogTimestamp ()
			{
			if (DateTime.Today > lastNotStamp)
				{
				if (NotificationsSupport.MasterLog != "")
					{
					// Добавление отступа
					if (lastNotStamp.Year == 2000)
						NotificationsSupport.MasterLog = Localization.GetText ("EarlierMessage", al) +
							"\r\n\r\n" + NotificationsSupport.MasterLog;
					else
						NotificationsSupport.MasterLog = "--- " +
							lastNotStamp.ToString (ci.DateTimeFormat.LongDatePattern, ci) +
							" ---\r\n\r\n" + NotificationsSupport.MasterLog;
					}

				lastNotStamp = DateTime.Today;
				}
			}

		// Запрос следующей новости
		private string GetNot ()
			{
			return ProgramDescription.NSet.GetNextNotification (false);
			}

		private async void NextNewsItem (object sender, EventArgs e)
			{
			// Подсказка
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.NewsItemTip))
				{
				ShowTips (NotificationsSupport.TipTypes.NewsItemTip, logPage);
				return;
				}

			// Блокировка
			getGMJButton.IsEnabled = nextNewsButton.IsEnabled = allNewsButton.IsEnabled = notificationButton.IsEnabled =
				shareButton.IsEnabled = false;
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestStarted", al),
				ToastLength.Short).Show ();

			// Запрос
			NotificationsSupport.StopRequested = false; // Разблокировка метода GetHTML
			string newText = await Task.Run<string> (GetNot);

			if ((newText == "") || (newText == "\x1"))
				{
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestFailed", al),
					ToastLength.Long).Show ();
				}
			else
				{
				// Запись в журнал
				UpdateLogTimestamp ();
				mainLog.Text = NotificationsSupport.MasterLog = newText + "\r\n\r\n\r\n" + NotificationsSupport.MasterLog;
				}

			// Разблокировка
			getGMJButton.IsEnabled = nextNewsButton.IsEnabled = allNewsButton.IsEnabled = notificationButton.IsEnabled =
				shareButton.IsEnabled = true;
			}

		// Запрос всех новостей
		private string GetAllNot ()
			{
			// Оболочка с включённой в неё паузой (иначе блокируется интерфейсный поток)
			Thread.Sleep ((int)ProgramDescription.MasterTimerDelay / 2);
			return ProgramDescription.NSet.GetNextNotification (true);
			}

		private async void AllNewsItems (object sender, EventArgs e)
			{
			// Подсказка
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.AllNewsItemsTip))
				{
				ShowTips (NotificationsSupport.TipTypes.AllNewsItemsTip, logPage);
				return;
				}

			// Блокировка
			getGMJButton.IsEnabled = nextNewsButton.IsEnabled = allNewsButton.IsEnabled = notificationButton.IsEnabled =
				shareButton.IsEnabled = false;
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestAllStarted", al),
				ToastLength.Long).Show ();

			// Запрос
			NotificationsSupport.StopRequested = false; // Разблокировка метода GetHTML
			ProgramDescription.NSet.ResetTimer (false); // Без сброса текстов
			string newText = "";

			// Опрос с защитой от закрытия приложения до завершения опроса
			while (NotificationsSupport.AppIsRunning && ((newText = await Task.Run<string> (GetAllNot)) != "\x1"))
				{
				if (newText != "")
					{
					// Запись в журнал
					UpdateLogTimestamp ();
					mainLog.Text = NotificationsSupport.MasterLog = newText + "\r\n\r\n\r\n" + NotificationsSupport.MasterLog;
					}
				}

			// Разблокировка
			getGMJButton.IsEnabled = nextNewsButton.IsEnabled = allNewsButton.IsEnabled = notificationButton.IsEnabled =
				shareButton.IsEnabled = true;
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestCompleted", al),
				ToastLength.Long).Show ();
			}

		// Включение / выключение светодиода
		private void ReadModeSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			if (e != null)
				NotificationsSupport.LogReadingMode = readModeSwitch.IsToggled;

			if (readModeSwitch.IsToggled)
				{
				logPage.BackgroundColor = mainLog.BackgroundColor = logReadModeColor;
				notificationButton.TextColor = shareButton.TextColor = nextNewsButton.TextColor = allNewsButton.TextColor =
					getGMJButton.TextColor = mainLog.TextColor = shareOffsetButton.TextColor = logMasterBackColor;
				}
			else
				{
				logPage.BackgroundColor = mainLog.BackgroundColor = logMasterBackColor;
				notificationButton.TextColor = shareButton.TextColor = nextNewsButton.TextColor = allNewsButton.TextColor =
					getGMJButton.TextColor = mainLog.TextColor = shareOffsetButton.TextColor = logReadModeColor;
				}
			}

		// Изменение значения частоты опроса
		private void FontSizeChanged (object sender, EventArgs e)
			{
			if (e != null)
				{
				Xamarin.Forms.Button b = (Xamarin.Forms.Button)sender;
				if ((b.Text == AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Increase)) &&
					(mainLog.FontSize < NotificationsSupport.MaxFontSize))
					mainLog.FontSize += 1;
				else if ((b.Text == AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Decrease)) &&
					(mainLog.FontSize > NotificationsSupport.MinFontSize))
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
			if (shareOffset < NotificationsSupport.MaxShareOffset)
				shareOffset++;
			else
				shareOffset = 1;

			shareOffsetButton.Text = shareOffset.ToString ();
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
				ProgramDescription.NSet.ResetTimer (true);
				NotificationsSupport.StopRequested = true;
				}

			ProgramDescription.NSet.SaveNotifications ();
			NotificationsSupport.AppIsRunning = false;

			try
				{
				Localization.CurrentLanguage = al;
				}
			catch
				{
				}
			}

		/// <summary>
		/// Возврат в интерфейс
		/// </summary>
		protected override void OnResume ()
			{
			NotificationsSupport.AppIsRunning = true;
			}
		}
	}
