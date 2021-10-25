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
	public partial class App:Application
		{
		#region Общие переменные и константы

		private SupportedLanguages al = Localization.CurrentLanguage;
		private List<MainLogItem> masterLog = new List<MainLogItem> (NotificationsSupport.MasterLog);

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

		private ContentPage settingsPage, notSettingsPage, aboutPage, logPage;
		private Label aboutLabel, occFieldLabel, fontSizeFieldLabel, requestStepFieldLabel,
			allowSoundLabel, allowLightLabel, allowVibroLabel;
		private Xamarin.Forms.Switch allowStart, enabledSwitch, readModeSwitch, rightAlignmentSwitch,
			allowSoundSwitch, allowLightSwitch, allowVibroSwitch;
		private Xamarin.Forms.Button selectedNotification, applyButton, addButton, deleteButton, getGMJButton,
			allNewsButton, notWizardButton;
		private Editor nameField, beginningField, endingField;
		private string linkField;
		private Xamarin.Forms.ListView mainLog;
		private uint currentOcc;
		private Grid mainGrid;

		#endregion

		/// <summary>
		/// Конструктор. Точка входа приложения
		/// </summary>
		public App ()
			{
			// Инициализация
			InitializeComponent ();
			if (ProgramDescription.NSet == null)
				ProgramDescription.NSet = new NotificationsSet (false);

			// Переход в статус запуска для отмены вызова из оповещения
			AndroidSupport.AppIsRunning = true;

			// Общая конструкция страниц приложения
			MainPage = new MasterPage ();

			settingsPage = AndroidSupport.ApplyPageSettings (MainPage, "SettingsPage",
				Localization.GetText ("SettingsPage", al), solutionMasterBackColor);
			notSettingsPage = AndroidSupport.ApplyPageSettings (MainPage, "NotSettingsPage",
				Localization.GetText ("NotSettingsPage", al), solutionMasterBackColor);
			aboutPage = AndroidSupport.ApplyPageSettings (MainPage, "AboutPage",
				Localization.GetText ("AboutPage", al), aboutMasterBackColor);
			logPage = AndroidSupport.ApplyPageSettings (MainPage, "LogPage",
				Localization.GetText ("LogPage", al), logMasterBackColor);

			int tab = 0;
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.PolicyTip))
				tab = 2;
			((CarouselPage)MainPage).CurrentPage = ((CarouselPage)MainPage).Children[tab];

			#region Настройки службы

			AndroidSupport.ApplyLabelSettingsForKKT (settingsPage, "ServiceSettingsLabel",
				Localization.GetText ("ServiceSettingsLabel", al), true);

			AndroidSupport.ApplyLabelSettingsForKKT (settingsPage, "AllowStartLabel",
				Localization.GetText ("AllowStartLabel", al), false);
			allowStart = (Xamarin.Forms.Switch)settingsPage.FindByName ("AllowStartSwitch");
			allowStart.IsToggled = AndroidSupport.AllowServiceToStart;
			allowStart.Toggled += AllowStart_Toggled;

			AndroidSupport.ApplyLabelSettingsForKKT (settingsPage, "NotWizardLabel",
				Localization.GetText ("NotWizardLabel", al), true);
			notWizardButton = AndroidSupport.ApplyButtonSettings (settingsPage, "NotWizardButton",
				Localization.GetText ("NotWizardButton", al), solutionFieldBackColor, StartNotificationsWizard);

			allowSoundLabel = AndroidSupport.ApplyLabelSettingsForKKT (settingsPage, "AllowSoundLabel",
				Localization.GetText ("AllowSoundLabel", al), false);
			allowSoundSwitch = (Xamarin.Forms.Switch)settingsPage.FindByName ("AllowSoundSwitch");
			allowSoundSwitch.IsToggled = NotificationsSupport.AllowSound;
			allowSoundSwitch.Toggled += AllowSound_Toggled;

			allowLightLabel = AndroidSupport.ApplyLabelSettingsForKKT (settingsPage, "AllowLightLabel",
				Localization.GetText ("AllowLightLabel", al), false);
			allowLightSwitch = (Xamarin.Forms.Switch)settingsPage.FindByName ("AllowLightSwitch");
			allowLightSwitch.IsToggled = NotificationsSupport.AllowLight;
			allowLightSwitch.Toggled += AllowLight_Toggled;

			allowVibroLabel = AndroidSupport.ApplyLabelSettingsForKKT (settingsPage, "AllowVibroLabel",
				Localization.GetText ("AllowVibroLabel", al), false);
			allowVibroSwitch = (Xamarin.Forms.Switch)settingsPage.FindByName ("AllowVibroSwitch");
			allowVibroSwitch.IsToggled = NotificationsSupport.AllowVibro;
			allowVibroSwitch.Toggled += AllowVibro_Toggled;

			allowLightLabel.IsVisible = allowLightSwitch.IsVisible = allowSoundLabel.IsVisible =
				allowSoundSwitch.IsVisible = allowVibroLabel.IsVisible = allowVibroSwitch.IsVisible =
				AndroidSupport.AreNotificationsConfigurable;

			#endregion

			#region Настройки оповещений

			selectedNotification = AndroidSupport.ApplyButtonSettings (notSettingsPage, "SelectedNotification",
				"", solutionFieldBackColor, SelectNotification);

			AndroidSupport.ApplyLabelSettingsForKKT (notSettingsPage, "NameFieldLabel",
				Localization.GetText ("NameFieldLabel", al), false);
			nameField = AndroidSupport.ApplyEditorSettings (notSettingsPage, "NameField", solutionFieldBackColor,
				Keyboard.Default, Notification.MaxBeginningEndingLength, "", null);
			nameField.Placeholder = Localization.GetText ("NameFieldPlaceholder", al);

			AndroidSupport.ApplyLabelSettingsForKKT (notSettingsPage, "LinkFieldLabel",
				Localization.GetText ("LinkFieldLabel", al), false);
			AndroidSupport.ApplyButtonSettings (notSettingsPage, "LinkFieldButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Select),
				solutionFieldBackColor, SpecifyNotificationLink);

			AndroidSupport.ApplyLabelSettingsForKKT (notSettingsPage, "BeginningFieldLabel",
				Localization.GetText ("BeginningFieldLabel", al), false);
			beginningField = AndroidSupport.ApplyEditorSettings (notSettingsPage, "BeginningField", solutionFieldBackColor,
				Keyboard.Url, Notification.MaxBeginningEndingLength, "", null);

			AndroidSupport.ApplyLabelSettingsForKKT (notSettingsPage, "EndingFieldLabel",
				Localization.GetText ("EndingFieldLabel", al), false);
			endingField = AndroidSupport.ApplyEditorSettings (notSettingsPage, "EndingField", solutionFieldBackColor,
				Keyboard.Url, Notification.MaxBeginningEndingLength, "", null);

			occFieldLabel = AndroidSupport.ApplyLabelSettingsForKKT (notSettingsPage, "OccFieldLabel", "", false);
			AndroidSupport.ApplyButtonSettings (notSettingsPage, "OccIncButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Increase),
				solutionFieldBackColor, OccurrenceChanged);
			AndroidSupport.ApplyButtonSettings (notSettingsPage, "OccDecButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Decrease),
				solutionFieldBackColor, OccurrenceChanged);
			currentOcc = 1;

			AndroidSupport.ApplyLabelSettingsForKKT (notSettingsPage, "EnabledLabel",
				Localization.GetText ("EnabledLabel", al), false);
			enabledSwitch = (Xamarin.Forms.Switch)notSettingsPage.FindByName ("EnabledSwitch");

			// Инициализация полей
			SelectNotification (null, null);

			#endregion

			#region Управление оповещениями

			applyButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ApplyButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Apply),
				solutionFieldBackColor, ApplyNotification);
			addButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "AddButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Create),
				solutionFieldBackColor, AddNotification);
			deleteButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "DeleteButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Delete),
				solutionFieldBackColor, DeleteNotification);

			AndroidSupport.ApplyButtonSettings (notSettingsPage, "ShareTemplateButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Share),
				solutionFieldBackColor, ShareTemplate);

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

			UpdateNotButtons ();

			AndroidSupport.ApplyButtonSettings (aboutPage, "LanguageSelector", Localization.LanguagesNames[(int)al],
				aboutFieldBackColor, SelectLanguage_Clicked);
			AndroidSupport.ApplyLabelSettingsForKKT (aboutPage, "LanguageLabel",
				Localization.GetText ("LanguageLabel", al), false);

			#endregion

			#region Страница лога приложения

			mainLog = (Xamarin.Forms.ListView)logPage.FindByName ("MainLog");
			mainLog.BackgroundColor = logFieldBackColor;
			mainLog.HasUnevenRows = true;
			mainLog.ItemTapped += MainLog_ItemTapped;
			mainLog.ItemTemplate = new DataTemplate (typeof (NotificationView));
			mainLog.SelectionMode = ListViewSelectionMode.None;
			mainLog.SeparatorVisibility = SeparatorVisibility.None;

			allNewsButton = AndroidSupport.ApplyButtonSettings (logPage, "AllNewsButton",
				Localization.GetText ("AllNewsButton", al), logFieldBackColor, AllNewsItems);
			allNewsButton.Margin = new Thickness (0);

			getGMJButton = AndroidSupport.ApplyButtonSettings (logPage, "GetGMJ",
				Localization.GetText ("GMJButton", al), logFieldBackColor, GetGMJ);
			getGMJButton.Margin = new Thickness (0);
			getGMJButton.IsVisible = ((al == SupportedLanguages.ru_ru));

			#endregion

			#region Прочие настройки

			AndroidSupport.ApplyLabelSettingsForKKT (settingsPage, "LogSettingsLabel",
				Localization.GetText ("LogSettingsLabel", al), true);

			AndroidSupport.ApplyLabelSettingsForKKT (settingsPage, "ReadModeLabel",
				Localization.GetText ("ReadModeLabel", al), false);
			readModeSwitch = (Xamarin.Forms.Switch)settingsPage.FindByName ("ReadModeSwitch");
			readModeSwitch.IsToggled = NotificationsSupport.LogReadingMode;
			readModeSwitch.Toggled += ReadModeSwitch_Toggled;
			ReadModeSwitch_Toggled (null, null);

			AndroidSupport.ApplyLabelSettingsForKKT (settingsPage, "RightAlignmentLabel",
				Localization.GetText ("RightAlignmentLabel", al), false);
			rightAlignmentSwitch = (Xamarin.Forms.Switch)settingsPage.FindByName ("RightAlignmentSwitch");
			rightAlignmentSwitch.IsToggled = NotificationsSupport.LogButtonsOnTheRightSide;
			rightAlignmentSwitch.Toggled += RightAlignmentSwitch_Toggled;

			mainGrid = (Grid)logPage.FindByName ("MainGrid");
			RightAlignmentSwitch_Toggled (null, null);

			fontSizeFieldLabel = AndroidSupport.ApplyLabelSettingsForKKT (settingsPage, "FontSizeFieldLabel", "", false);
			AndroidSupport.ApplyButtonSettings (settingsPage, "FontSizeIncButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Increase),
				solutionFieldBackColor, FontSizeChanged);
			AndroidSupport.ApplyButtonSettings (settingsPage, "FontSizeDecButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Decrease),
				solutionFieldBackColor, FontSizeChanged);
			FontSizeChanged (null, null);

			requestStepFieldLabel = AndroidSupport.ApplyLabelSettingsForKKT (settingsPage, "RequestStepFieldLabel", "", false);
			AndroidSupport.ApplyButtonSettings (settingsPage, "RequestStepIncButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Increase),
				solutionFieldBackColor, RequestStepChanged);
			AndroidSupport.ApplyButtonSettings (settingsPage, "RequestStepDecButton",
				AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Decrease),
				solutionFieldBackColor, RequestStepChanged);
			RequestStepChanged (null, null);

			#endregion

			// Запуск цикла обратной связи (без ожидания)
			FinishBackgroundRequest ();

			// Принятие соглашений
			ShowStartupTips ();
			}

		// Цикл обратной связи для загрузки текущего журнала, если фоновая служба не успела завершить работу
		private async Task<bool> FinishBackgroundRequest ()
			{
			if (!NotificationsSupport.BackgroundRequestInProgress)
				return false;

			// Ожидание завершения операции
			SetLogState (false);
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("BackgroundRequestInProgress", al),
				ToastLength.Long).Show ();
			await Task<bool>.Run (WaitForFinishingRequest);

			// Перезапрос журнала
			masterLog.Clear ();
			masterLog = new List<MainLogItem> (NotificationsSupport.MasterLog);
			UpdateLog ();

			SetLogState (true);
			return true;
			}

		// Отвязанный от текущего контекста нагрузочный процесс с запросом
		private bool WaitForFinishingRequest ()
			{
			while (NotificationsSupport.BackgroundRequestInProgress && AndroidSupport.AppIsRunning)
				Thread.Sleep ((int)ProgramDescription.MasterFrameLength);
			return true;
			}

		// Выбор оповещения для перехода или share
		private async void MainLog_ItemTapped (object sender, ItemTappedEventArgs e)
			{
			// Контроль
			MainLogItem notItem = (MainLogItem)e.Item;
			if (!allNewsButton.IsEnabled || (notItem.StringForSaving == ""))  // Признак разделителя
				return;

			// Запрос варианта использования
			List<string> items = new List<string> {
				Localization.GetText ("GoToOption", al),
				Localization.GetText ("ShareOption", al),
				Localization.GetText ("RequestAgainOption",al),
				Localization.GetText ("OtherOption", al)
				};
			string res = await logPage.DisplayActionSheet (Localization.GetText ("SelectOption", al),
				Localization.GetText ("CancelButton", al), null, items.ToArray ());
			if (!items.Contains (res))
				{
				items.Clear ();
				return;
				}

			int variant = items.IndexOf (res);
			items.Clear ();

			// Контроль второго набора
			if (variant > 2)
				{
				items = new List<string> {
					Localization.GetText ("RemoveOption", al),
					Localization.GetText ("DisableOption", al)
					};
				res = await logPage.DisplayActionSheet (Localization.GetText ("SelectOption", al),
					Localization.GetText ("CancelButton", al), null, items.ToArray ());
				if (!items.Contains (res))
					{
					items.Clear ();
					return;
					}

				variant += items.IndexOf (res);
				items.Clear ();
				}

			// Извлечение текста и ссылки
			string notLink = "";
			int notNumber = -1;
			if (notItem.Header.Contains (GMJ.GMJName))
				{
				notLink = GMJ.GMJRedirectLink;
				}
			else
				{
				for (notNumber = 0; notNumber < ProgramDescription.NSet.Notifications.Count; notNumber++)
					if (notItem.Header.Contains (ProgramDescription.NSet.Notifications[notNumber].Name))
						{
						notLink = ProgramDescription.NSet.Notifications[notNumber].Link;
						break;
						}
				}

			// Обработка (неподходящие варианты будут отброшены)
			switch (variant)
				{
				// Переход по ссылке
				case 0:
					if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.GoToButton))
						await ShowTips (NotificationsSupport.TipTypes.GoToButton, logPage);

					try
						{
						await Launcher.OpenAsync (notLink);
						}
					catch
						{
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("WebIsUnavailable", al),
							ToastLength.Long).Show ();
						}
					break;

				// Поделиться
				case 1:
					if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.ShareButton))
						await ShowTips (NotificationsSupport.TipTypes.ShareButton, logPage);

					await Share.RequestAsync ((notItem.Header + "\n\n" + notItem.Text + "\n\n" + notLink).Replace ("\r", ""),
						ProgramDescription.AssemblyVisibleName);
					break;

				// Повторный опрос
				case 2:
					// Проверка
					if ((notNumber < 0) || (notNumber >= ProgramDescription.NSet.Notifications.Count))
						{
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("ActionIsUnsupported", al),
							ToastLength.Long).Show ();
						break;
						}

					// Блокировка
					SetLogState (false);
					Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestStarted", al),
						ToastLength.Long).Show ();

					// Запрос
					AndroidSupport.StopRequested = false;           // Разблокировка метода GetHTML
					ProgramDescription.NSet.ResetTimer (false);     // Без сброса текстов
					string newText = "";

					// Опрос с защитой от закрытия приложения до завершения опроса
					if (AndroidSupport.AppIsRunning &&
						((newText = await ProgramDescription.NSet.GetNextNotification (notNumber)) != ""))
						{
						// Запись в журнал
						AddTextToLog (newText);
						UpdateLog ();
						}

					// Разблокировка
					SetLogState (true);
					Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestCompleted", al),
						ToastLength.Long).Show ();
					break;

				// Удаление из журнала
				case 3:
					masterLog.RemoveAt (e.ItemIndex);
					UpdateLog ();
					break;

				// Отключение оповещения
				case 4:
					// Проверка
					if ((notNumber < 0) || (notNumber >= ProgramDescription.NSet.Notifications.Count))
						{
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("ActionIsUnsupported", al),
							ToastLength.Long).Show ();
						break;
						}

					// Применение
					currentNotification = notNumber;
					SelectNotification (null, null);
					enabledSwitch.IsToggled = false;
					ApplyNotification (null, null);
					break;
				}

			// Завершено
			}

		// Включение / выключение службы
		private async void AllowStart_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.ServiceLaunchTip))
				await ShowTips (NotificationsSupport.TipTypes.ServiceLaunchTip, settingsPage);

			AndroidSupport.AllowServiceToStart = allowStart.IsToggled;
			}

		// Включение / выключение вариантов индикации
		private async void AllowSound_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.IndicationTip))
				await ShowTips (NotificationsSupport.TipTypes.IndicationTip, settingsPage);

			NotificationsSupport.AllowSound = allowSoundSwitch.IsToggled;
			}

		private async void AllowLight_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.IndicationTip))
				await ShowTips (NotificationsSupport.TipTypes.IndicationTip, settingsPage);

			NotificationsSupport.AllowLight = allowLightSwitch.IsToggled;
			}

		private async void AllowVibro_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.IndicationTip))
				await ShowTips (NotificationsSupport.TipTypes.IndicationTip, settingsPage);

			NotificationsSupport.AllowVibro = allowVibroSwitch.IsToggled;
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
		private async void ShowStartupTips ()
			{
			// Требование принятия Политики
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.PolicyTip))
				{
				while (!await notSettingsPage.DisplayAlert (ProgramDescription.AssemblyTitle,
					Localization.GetText ("PolicyMessage", al),
					Localization.GetText ("AcceptButton", al),
					Localization.GetText ("DeclineButton", al)))
					{
					ADPButton_Clicked (null, null);
					}
				NotificationsSupport.SetTipState (NotificationsSupport.TipTypes.PolicyTip);
				}

			// Подсказки
			if (NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.StartupTips))
				return;

			await notSettingsPage.DisplayAlert (Localization.GetText ("TipHeader01", al),
				Localization.GetText ("Tip01", al), Localization.GetText ("NextButton", al));

			await notSettingsPage.DisplayAlert (Localization.GetText ("TipHeader01", al),
				Localization.GetText ("Tip02", al), Localization.GetText ("NextButton", al));

			string tip03 = Localization.GetText ("Tip03_1", al);
			if (!AndroidSupport.AreNotificationsConfigurable)
				tip03 += Localization.GetText ("Tip03_2", al);
			await notSettingsPage.DisplayAlert (Localization.GetText ("TipHeader01", al),
				tip03, Localization.GetText ("NextButton", al));

			NotificationsSupport.SetTipState (NotificationsSupport.TipTypes.StartupTips);
			}

		// Метод отображает остальные подсказки
		private async Task<bool> ShowTips (NotificationsSupport.TipTypes Type, Page DisplayPage)
			{
			// Подсказки
			await DisplayPage.DisplayAlert (Localization.GetText ("TipHeader01", al),
				Localization.GetText ("Tip04_" + ((int)Type).ToString (), al), Localization.GetText ("NextButton", al));

			NotificationsSupport.SetTipState (Type);
			return true;
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

		// Страница лаборатории
		private async void CommunityButton_Clicked (object sender, EventArgs e)
			{
			List<string> comm = new List<string> {
				Localization.GetText ("CommunityVK", al), Localization.GetText ("CommunityTG", al) };
			string res = await aboutPage.DisplayActionSheet (Localization.GetText ("CommunitySelect", al),
				Localization.GetText ("CancelButton", al), null, comm.ToArray ());

			if (!comm.Contains (res))
				return;

			try
				{
				if (comm.IndexOf (res) == 0)
					await Launcher.OpenAsync (AndroidSupport.MasterCommunityLink);
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
					To = new List<string> () { AndroidSupport.MasterDeveloperLink }
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
				await ShowTips (NotificationsSupport.TipTypes.CurrentNotButton, notSettingsPage);

			// Запрос списка оповещений
			List<string> list = new List<string> ();
			foreach (Notification element in ProgramDescription.NSet.Notifications)
				list.Add (element.Name);

			string res = list[currentNotification];
			if (e != null)
				res = await notSettingsPage.DisplayActionSheet (Localization.GetText ("SelectNotification", al),
					Localization.GetText ("CancelButton", al), null, list.ToArray ());

			// Установка результата
			int i = currentNotification;
			if ((e == null) || ((i = list.IndexOf (res)) >= 0))
				{
				currentNotification = i;
				selectedNotification.Text = res;

				nameField.Text = ProgramDescription.NSet.Notifications[i].Name;
				linkField = ProgramDescription.NSet.Notifications[i].Link;
				beginningField.Text = ProgramDescription.NSet.Notifications[i].Beginning;
				endingField.Text = ProgramDescription.NSet.Notifications[i].Ending;
				currentOcc = ProgramDescription.NSet.Notifications[i].OccurrenceNumber;
				OccurrenceChanged (null, null);
				enabledSwitch.IsToggled = ProgramDescription.NSet.Notifications[i].IsEnabled;
				}

			// Сброс
			list.Clear ();
			}
		private int currentNotification = 0;

		// Блокировка / разблокировка кнопок
		private void SetLogState (bool State)
			{
			getGMJButton.IsEnabled = allNewsButton.IsEnabled = State;
			}

		// Запрос записи из GMJ
		private async void GetGMJ (object sender, EventArgs e)
			{
			// Блокировка на время опроса
			SetLogState (false);
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestStarted", al),
				ToastLength.Short).Show ();

			// Запуск и разбор
			AndroidSupport.StopRequested = false; // Разблокировка метода GetHTML
			string newText = await Task.Run<string> (GMJ.GetRandomGMJ);

			if (newText == "")
				{
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("GMJRequestFailed", al),
					ToastLength.Long).Show ();
				}
			else if (newText.Contains (GMJ.GMJName + " не вернула"))
				{
				Toast.MakeText (Android.App.Application.Context, newText, ToastLength.Long).Show ();
				}
			else
				{
				AddTextToLog (newText);
				UpdateLog ();
				}

			// Разблокировка
			SetLogState (true);
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.MainLogClickMenuTip))
				await ShowTips (NotificationsSupport.TipTypes.MainLogClickMenuTip, logPage);
			}

		// Добавление текста в журнал
		private void AddTextToLog (string Text)
			{
			masterLog.Insert (0, new MainLogItem (Text));

			// Удаление нижних строк (здесь требуется, т.к. не выполняется обрезка свойством .MainLog)
			while (masterLog.Count >= ProgramDescription.MasterLogMaxItems)
				masterLog.RemoveAt (masterLog.Count - 1);
			}

		// Изменение порядкового номера вхождения
		private async void OccurrenceChanged (object sender, EventArgs e)
			{
			// Изменение значения
			if (sender != null)
				{
				// Подсказки
				if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.OccurenceTip))
					await ShowTips (NotificationsSupport.TipTypes.OccurenceTip, notSettingsPage);

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
				await ShowTips (NotificationsSupport.TipTypes.DeleteButton, notSettingsPage);

			// Контроль
			if (!await notSettingsPage.DisplayAlert (ProgramDescription.AssemblyVisibleName,
				Localization.GetText ("DeleteMessage", al), Localization.GetText ("NextButton", al),
				Localization.GetText ("CancelButton", al)))
				return;

			// Удаление и переход к другому оповещению
			ProgramDescription.NSet.Notifications.RemoveAt (currentNotification);
			if (currentNotification >= ProgramDescription.NSet.Notifications.Count)
				currentNotification = ProgramDescription.NSet.Notifications.Count - 1;
			SelectNotification (null, null);

			// Обновление контролов
			UpdateNotButtons ();
			}

		// Добавление нового оповещения
		private async void AddNotification (object sender, EventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.AddButton))
				await ShowTips (NotificationsSupport.TipTypes.AddButton, notSettingsPage);

			// Добавление (требует ожидания для корректного отображения сообщений)
			// При успехе – выбор нового оповещения
			if (await UpdateItem (-1))
				{
				currentNotification = ProgramDescription.NSet.Notifications.Count - 1;
				SelectNotification (null, null);

				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("AddAsNewMessage", al) + nameField.Text,
					ToastLength.Short).Show ();
				}
			}

		// Обновление оповещения
		private async void ApplyNotification (object sender, EventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.ApplyButton))
				await ShowTips (NotificationsSupport.TipTypes.ApplyButton, notSettingsPage);

			// Обновление (при успехе – обновление названия)
			if (await UpdateItem (currentNotification))
				{
				selectedNotification.Text = nameField.Text;

				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("ApplyMessage", al) + nameField.Text,
					ToastLength.Short).Show ();
				}
			}

		// Общий метод обновления оповещений
		private async Task<bool> UpdateItem (int ItemNumber)
			{
			// Инициализация оповещения
			Notification ni = new Notification (nameField.Text, linkField, beginningField.Text, endingField.Text,
				1, currentOcc);

			if (!ni.IsInited)
				{
				await notSettingsPage.DisplayAlert (ProgramDescription.AssemblyVisibleName,
					Localization.GetText ("NotEnoughDataMessage", al), Localization.GetText ("NextButton", al));

				nameField.Focus ();
				return false;
				}
			if ((ItemNumber < 0) && ProgramDescription.NSet.Notifications.Contains (ni)) // Не относится к обновлению позиции
				{
				await notSettingsPage.DisplayAlert (ProgramDescription.AssemblyVisibleName,
					Localization.GetText ("NotMatchingNames", al), Localization.GetText ("NextButton", al));

				nameField.Focus ();
				return false;
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
			UpdateNotButtons ();
			return true;
			}

		// Обновление кнопок
		private void UpdateNotButtons ()
			{
			addButton.IsVisible = notWizardButton.IsEnabled = (ProgramDescription.NSet.Notifications.Count <
				NotificationsSet.MaxNotifications);
			deleteButton.IsVisible = (ProgramDescription.NSet.Notifications.Count > 1);
			}

		// Метод загружает шаблон оповещения
		private List<string> templatesNames = new List<string> ();
		private async void StartNotificationsWizard (object sender, EventArgs e)
			{
			// Подсказки
			notWizardButton.IsEnabled = false;

			// Запрос варианта использования
			List<string> items = new List<string> {
				Localization.GetText ("NotificationsWizard", al),
				Localization.GetText ("TemplateList", al),
				Localization.GetText ("TemplateClipboard", al)
				};
			string res = await notSettingsPage.DisplayActionSheet (Localization.GetText ("TemplateSelect", al),
					Localization.GetText ("CancelButton", al), null, items.ToArray ());

			// Обработка (недопустимые значения будут отброшены)
			switch (items.IndexOf (res))
				{
				// Отмена
				default:
					notWizardButton.IsEnabled = true;
					return;

				// Мастер
				case 0:
					if (!await NotificationsWizard ())
						{
						notWizardButton.IsEnabled = true;
						return;
						}
					break;

				// Шаблон
				case 1:
					// Обновление списка шаблонов
					if (!NotificationsSupport.TemplatesForCurrentSessionAreUpdated)
						{
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("UpdatingTemplates", al),
							ToastLength.Long).Show ();

						await Task.Run<bool> (ProgramDescription.NSet.UpdateNotificationsTemplates);
						NotificationsSupport.TemplatesForCurrentSessionAreUpdated = true;
						}

					// Запрос списка шаблонов
					if (templatesNames.Count != ProgramDescription.NSet.NotificationsTemplates.TemplatesCount)
						{
						templatesNames.Clear ();
						for (uint i = 0; i < ProgramDescription.NSet.NotificationsTemplates.TemplatesCount; i++)
							templatesNames.Add (ProgramDescription.NSet.NotificationsTemplates.GetName (i));
						}

					res = await notSettingsPage.DisplayActionSheet (Localization.GetText ("SelectTemplate", al),
						Localization.GetText ("CancelButton", al), null, templatesNames.ToArray ());

					// Установка результата
					uint templateNumber = 0;
					int r;
					if ((r = templatesNames.IndexOf (res)) >= 0)
						{
						templateNumber = (uint)r;
						}
					else
						{
						notWizardButton.IsEnabled = true;
						return;
						}

					// Проверка
					if (ProgramDescription.NSet.NotificationsTemplates.IsTemplateIncomplete (templateNumber))
						await notSettingsPage.DisplayAlert (ProgramDescription.AssemblyVisibleName,
							Localization.GetText ("CurlyTemplate", al), Localization.GetText ("NextButton", al));

					// Заполнение
					nameField.Text = ProgramDescription.NSet.NotificationsTemplates.GetName (templateNumber);
					linkField = ProgramDescription.NSet.NotificationsTemplates.GetLink (templateNumber);
					beginningField.Text = ProgramDescription.NSet.NotificationsTemplates.GetBeginning (templateNumber);
					endingField.Text = ProgramDescription.NSet.NotificationsTemplates.GetEnding (templateNumber);
					currentOcc = ProgramDescription.NSet.NotificationsTemplates.GetOccurrenceNumber (templateNumber);

					break;

				// Разбор переданного шаблона
				case 2:
					// Запрос из буфера обмена
					string text = "";
					try
						{
						text = await Clipboard.GetTextAsync ();
						}
					catch { }

					if ((text == null) || (text == ""))
						{
						await notSettingsPage.DisplayAlert (ProgramDescription.AssemblyVisibleName,
							Localization.GetText ("NoTemplateInClipboard", al), Localization.GetText ("NextButton", al));
						notWizardButton.IsEnabled = true;
						return;
						}

					// Разбор
					string[] values = text.Split (NotificationsTemplatesProvider.ClipboardTemplateSplitter,
						StringSplitOptions.RemoveEmptyEntries);
					if (values.Length != 5)
						{
						await notSettingsPage.DisplayAlert (ProgramDescription.AssemblyVisibleName,
							Localization.GetText ("NoTemplateInClipboard", al), Localization.GetText ("NextButton", al));
						notWizardButton.IsEnabled = true;
						return;
						}

					// Заполнение
					nameField.Text = values[0];
					linkField = values[1];
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

					break;
				}

			// Обновление
			OccurrenceChanged (null, null);
			items.Clear ();

			// Создание уведомления
			enabledSwitch.IsToggled = true;
			if (await UpdateItem (-1))
				{
				currentNotification = ProgramDescription.NSet.Notifications.Count - 1;
				SelectNotification (null, null);

				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("AddAsNewMessage", al) + nameField.Text,
					ToastLength.Short).Show ();
				}

			notWizardButton.IsEnabled = true;
			}

		// Метод формирует и отправляет шаблон оповещения
		private async void ShareTemplate (object sender, EventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.ShareNotButton))
				await ShowTips (NotificationsSupport.TipTypes.ShareNotButton, notSettingsPage);

			// Формирование и отправка
			await Share.RequestAsync (nameField.Text + NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () +
					linkField + NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () +
					beginningField.Text + NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () +
					endingField.Text + NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () +
					currentOcc.ToString (),
					ProgramDescription.AssemblyVisibleName);
			}

		// Запрос всех новостей
		private async Task<string> GetNotification ()
			{
			// Оболочка с включённой в неё паузой (иначе блокируется интерфейсный поток)
			Thread.Sleep ((int)ProgramDescription.MasterFrameLength * 2);
			return await ProgramDescription.NSet.GetNextNotification (-1);
			}

		private async void AllNewsItems (object sender, EventArgs e)
			{
			// Проверка
			if (!await logPage.DisplayAlert (ProgramDescription.AssemblyVisibleName,
				Localization.GetText ("AllNewsRequest", al), Localization.GetText ("NextButton", al),
				Localization.GetText ("CancelButton", al)))
				return;

			// Блокировка
			SetLogState (false);
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestAllStarted", al),
				ToastLength.Long).Show ();

			// Запрос
			AndroidSupport.StopRequested = false; // Разблокировка метода GetHTML
			ProgramDescription.NSet.ResetTimer (false); // Без сброса текстов
			string newText = "";

			// Опрос с защитой от закрытия приложения до завершения опроса
			while (AndroidSupport.AppIsRunning && ((newText = await Task.Run<string> (GetNotification)) !=
				NotificationsSet.NoNewsSign))
				if (newText != "")
					{
					// Запись в журнал
					AddTextToLog (newText);
					UpdateLog ();
					}

			// Разблокировка
			SetLogState (true);
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestCompleted", al),
				ToastLength.Long).Show ();

			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.MainLogClickMenuTip))
				await ShowTips (NotificationsSupport.TipTypes.MainLogClickMenuTip, logPage);
			}

		// Включение / выключение режима чтения для лога
		private void ReadModeSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			if (e != null)
				NotificationsSupport.LogReadingMode = readModeSwitch.IsToggled;

			if (readModeSwitch.IsToggled)
				{
				logPage.BackgroundColor = mainLog.BackgroundColor = logReadModeColor;
				allNewsButton.TextColor = getGMJButton.TextColor = NotificationsSupport.LogFontColor = logMasterBackColor;
				}
			else
				{
				logPage.BackgroundColor = mainLog.BackgroundColor = logMasterBackColor;
				allNewsButton.TextColor = getGMJButton.TextColor = NotificationsSupport.LogFontColor = logReadModeColor;
				}

			// Принудительное обновление
			UpdateLog ();
			}

		// Включение / выключение режима чтения для лога
		private void RightAlignmentSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			if (e != null)
				NotificationsSupport.LogButtonsOnTheRightSide = rightAlignmentSwitch.IsToggled;

			mainGrid.FlowDirection = rightAlignmentSwitch.IsToggled ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
			}

		// Изменение значения частоты опроса
		private void FontSizeChanged (object sender, EventArgs e)
			{
			uint fontSize = NotificationsSupport.LogFontSize;

			if (e != null)
				{
				Xamarin.Forms.Button b = (Xamarin.Forms.Button)sender;
				if ((b.Text == AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Increase)) &&
					(fontSize < NotificationsSupport.MaxFontSize))
					fontSize++;
				else if ((b.Text == AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Decrease)) &&
					(fontSize > NotificationsSupport.MinFontSize))
					fontSize--;

				NotificationsSupport.LogFontSize = fontSize;
				}

			// Принудительное обновление
			fontSizeFieldLabel.Text = Localization.GetText ("FontSizeLabel", al) + fontSize.ToString ();
			UpdateLog ();
			}

		// Изменение значения частоты опроса
		private void RequestStepChanged (object sender, EventArgs e)
			{
			if (e != null)
				{
				Xamarin.Forms.Button b = (Xamarin.Forms.Button)sender;
				if ((b.Text == AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Increase)) &&
					(NotificationsSupport.BackgroundRequestStep < NotificationsSupport.MaxBackgroundRequestStep))
					NotificationsSupport.BackgroundRequestStep++;
				else if ((b.Text == AndroidSupport.GetDefaultButtonName (AndroidSupport.ButtonsDefaultNames.Decrease)) &&
					(NotificationsSupport.BackgroundRequestStep > 0))
					NotificationsSupport.BackgroundRequestStep--;
				}

			// Обновление
			if (NotificationsSupport.BackgroundRequestStep > 0)
				requestStepFieldLabel.Text = string.Format (Localization.GetText ("BackgroundRequestOn", al),
					NotificationsSupport.BackgroundRequestStep * NotificationsSupport.BackgroundRequestStepMinutes);
			else
				requestStepFieldLabel.Text = Localization.GetText ("BackgroundRequestOff", al);
			}

		// Принудительное обновление лога
		private void UpdateLog ()
			{
			mainLog.ItemsSource = null;
			mainLog.ItemsSource = masterLog;
			}

		// Выбор ссылки для оповещения
		private async void SpecifyNotificationLink (object sender, EventArgs e)
			{
			// Запрос
			string res = await notSettingsPage.DisplayPromptAsync (Localization.GetText ("LinkFieldLabel", al),
				null, Localization.GetText ("NextButton", al), Localization.GetText ("CancelButton", al),
				Localization.GetText ("LinkFieldPlaceholder", al), 150, Keyboard.Url, linkField);

			if (!string.IsNullOrWhiteSpace (res))
				linkField = res;
			}

		// Вызов помощника по созданию оповещений
		private async Task<bool> NotificationsWizard ()
			{
			// Шаг запроса ссылки
			string link = await settingsPage.DisplayPromptAsync (ProgramDescription.AssemblyVisibleName,
				Localization.GetText ("WizardStep1", al), Localization.GetText ("NextButton", al),
				Localization.GetText ("CancelButton", al), Localization.GetText ("LinkFieldPlaceholder", al),
				Notification.MaxLinkLength, Keyboard.Url, "");

			if (string.IsNullOrWhiteSpace (link))
				return false;

			// Шаг запроса ключевого слова
			string keyword = await settingsPage.DisplayPromptAsync (ProgramDescription.AssemblyVisibleName,
				Localization.GetText ("WizardStep2", al), Localization.GetText ("NextButton", al),
				Localization.GetText ("CancelButton", al), null,
				Notification.MaxBeginningEndingLength, Keyboard.Default, "");

			if (string.IsNullOrWhiteSpace (keyword))
				return false;

			// Запуск
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("WizardSearch1", al),
				ToastLength.Long).Show ();

			string[] delim = await Notification.FindDelimiters (link, keyword);
			if (delim == null)
				{
				await settingsPage.DisplayAlert (ProgramDescription.AssemblyVisibleName, Localization.GetText ("WizardFailure", al),
					Localization.GetText ("NextButton", al));
				return false;
				}

			// Попытка запроса
			uint occ;
			for (occ = 1; occ <= 3; occ++)
				{
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("WizardSearch2", al),
					ToastLength.Long).Show ();

				Notification not = new Notification ("Test", link, delim[0], delim[1], 1, occ);
				if (!await not.Update ())
					{
					await settingsPage.DisplayAlert (ProgramDescription.AssemblyVisibleName,
						Localization.GetText ("WizardFailure", al), Localization.GetText ("NextButton", al));
					return false;
					}

				// Получен текст, проверка
				string text = not.CurrentText;
				if (text.Length > 300)
					text = text.Substring (0, 297) + "...";
				if (await settingsPage.DisplayAlert (ProgramDescription.AssemblyVisibleName,
					Localization.GetText ((occ < 3) ? "WizardStep3" : "WizardStep4", al) + "\n\n" +
					"~".PadRight (10, '~') + "\n\n" + text,
					Localization.GetText ("NextButton", al),
					Localization.GetText ((occ < 3) ? "RetryButton" : "CancelButton", al)))
					{
					break;
					}
				else
					{
					if (occ < 3)
						continue;
					else
						return false;
					}
				}

			// Завершено, запрос названия
			string name = await settingsPage.DisplayPromptAsync (ProgramDescription.AssemblyVisibleName,
				Localization.GetText ("WizardStep5", al), Localization.GetText ("NextButton", al),
				Localization.GetText ("CancelButton", al), null,
				Notification.MaxBeginningEndingLength, Keyboard.Default, "");

			if (string.IsNullOrWhiteSpace (name))
				return false;

			// Добавление оповещения
			nameField.Text = name;
			linkField = link;
			beginningField.Text = delim[0];
			endingField.Text = delim[1];
			currentOcc = occ;

			return true;
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
				AndroidSupport.StopRequested = true;
				}

			ProgramDescription.NSet.SaveNotifications ();
			NotificationsSupport.MasterLog = masterLog;
			AndroidSupport.AppIsRunning = false;
			Localization.CurrentLanguage = al;
			}

		/// <summary>
		/// Возврат в интерфейс
		/// </summary>
		protected override void OnResume ()
			{
			AndroidSupport.AppIsRunning = true;
			}
		}
	}
