using System;
using System.Collections.Generic;
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

		// Главный журнал приложения
		private List<MainLogItem> masterLog;

		// Управление центральной кнопкой журнала
		private bool centerButtonEnabled = true;
		private const string semaphoreOn = "●";
		private const string semaphoreOff = "○";

		private uint maintenanceOpeningIndex = 0;

		// Методы сравнения для уведомлений, текущий метод и направление приращения порога
		private List<string> comparatorTypes;
		private NotComparatorTypes comparatorType = NotComparatorTypes.Equal;
		private bool comparatorValueIncreased = true;

		// Вкладка настроек опопвещений
		private const int notSettingsTab = 2;

		// Поля настроек оповещений
		private string linkField, beginningField, endingField;
		private uint currentOcc, currentFreq;

		// Флаг направления приращения интервалов
		private bool requestStepIncreased = true;

		// Индекс текущей опрашиваемой новости
		private int getNotificationIndex = -1;

		// Флаг завершения прокрутки журнала
		private bool needsScroll = true;

		// Сформированные контекстные меню
		private List<List<string>> tapMenuItems = new List<List<string>> ();
		private List<string> templatesNames = new List<string> ();
		private List<string> wizardMenuItems = new List<string> ();
		private List<string> specialOptions = new List<string> ();
		private List<string> templatesMenuItems = new List<string> ();

		// Текущее настраиваемое уведомление
		private int currentNotification = 0;

		// Цветовая схема
		private readonly Color
			logMasterBackColor = Color.FromHex ("#F0F0F0"),
			logFieldBackColor = Color.FromHex ("#80808080"),
			logReadModeColor = Color.FromHex ("#202020"),

			settingsMasterBackColor = Color.FromHex ("#FFF8F0"),
			settingsFieldBackColor = Color.FromHex ("#FFE8D0"),

			notSettingsMasterBackColor = Color.FromHex ("#F0F8FF"),
			notSettingsFieldBackColor = Color.FromHex ("#D0E8FF"),

			solutionLockedBackColor = Color.FromHex ("#F0F0F0"),

			aboutMasterBackColor = Color.FromHex ("#F0FFF0"),
			aboutFieldBackColor = Color.FromHex ("#D0FFD0");

		#endregion

		#region Переменные страниц

		private ContentPage settingsPage, notSettingsPage, aboutPage, logPage;

		private Label aboutLabel, occFieldLabel, fontSizeFieldLabel, requestStepFieldLabel,
			allowSoundLabel, allowLightLabel, allowVibroLabel, comparatorLabel, ignoreMisfitsLabel,
			aboutFontSizeField;

		private Xamarin.Forms.Switch allowStart, enabledSwitch, nightModeSwitch,
			allowSoundSwitch, allowLightSwitch, allowVibroSwitch, indicateOnlyUrgentSwitch,
			comparatorSwitch, ignoreMisfitsSwitch, notifyIfUnavailableSwitch, newsAtTheEndSwitch,
			keepScreenOnSwitch;

		private Xamarin.Forms.Button selectedNotification, applyButton, deleteButton,
			notWizardButton, comparatorTypeButton, comparatorIncButton,
			comparatorLongButton, comparatorDecButton, centerButtonFunction, linkFieldButton,
			centerButton, languageButton, scrollUpButton, scrollDownButton;

		private Editor nameField, comparatorValueField;

		private Xamarin.Forms.ListView mainLog;

		#endregion

		#region Запуск и работа приложения

		/// <summary>
		/// Конструктор. Точка входа приложения
		/// </summary>
		public App (bool Huawei)
			{
			// Инициализация
			InitializeComponent ();

			if (ProgramDescription.NSet == null)
				ProgramDescription.NSet = new NotificationsSet (false);
			ProgramDescription.NSet.HasUrgentNotifications = false;

			// Переход в статус запуска для отмены вызова из оповещения
			AndroidSupport.AppIsRunning = true;

			// Список типов компараторов
			char[] ctSplitter = new char[] { '\n' };
			comparatorTypes = new List<string> (Localization.GetText ("ComparatorTypes").Split (ctSplitter));

			#region Общая конструкция страниц приложения

			MainPage = new MasterPage ();

			settingsPage = AndroidSupport.ApplyPageSettings (MainPage, "SettingsPage",
				Localization.GetText ("SettingsPage"), settingsMasterBackColor);
			notSettingsPage = AndroidSupport.ApplyPageSettings (MainPage, "NotSettingsPage",
				Localization.GetText ("NotSettingsPage"), notSettingsMasterBackColor);
			aboutPage = AndroidSupport.ApplyPageSettings (MainPage, "AboutPage",
				Localization.GetDefaultText (LzDefaultTextValues.Control_AppAbout), aboutMasterBackColor);
			logPage = AndroidSupport.ApplyPageSettings (MainPage, "LogPage",
				Localization.GetText ("LogPage"), logMasterBackColor);
			AndroidSupport.SetMainPage (MainPage);

			#endregion

			int tab = 0;
			if (!NotificationsSupport.GetTipState (NSTipTypes.PolicyTip))
				tab = notSettingsTab;
			((CarouselPage)MainPage).CurrentPage = ((CarouselPage)MainPage).Children[tab];

			#region Настройки службы

			AndroidSupport.ApplyLabelSettings (settingsPage, "ServiceSettingsLabel",
				Localization.GetText ("ServiceSettingsLabel"), ASLabelTypes.HeaderLeft);

			AndroidSupport.ApplyLabelSettings (settingsPage, "AllowStartLabel",
				Localization.GetText ("AllowStartLabel"), ASLabelTypes.DefaultLeft);
			allowStart = AndroidSupport.ApplySwitchSettings (settingsPage, "AllowStartSwitch",
				false, settingsFieldBackColor, AllowStart_Toggled, AndroidSupport.AllowServiceToStart);

			AndroidSupport.ApplyLabelSettings (settingsPage, "NotWizardLabel",
				Localization.GetText ("NotWizardLabel"), ASLabelTypes.HeaderLeft);
			notWizardButton = AndroidSupport.ApplyButtonSettings (settingsPage, "NotWizardButton",
				Localization.GetText ("NotWizardButton"), settingsFieldBackColor, StartNotificationsWizard, false);

			allowSoundLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "AllowSoundLabel",
				Localization.GetText ("AllowSoundLabel"), ASLabelTypes.DefaultLeft);
			allowSoundSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "AllowSoundSwitch",
				false, settingsFieldBackColor, AllowSound_Toggled, NotificationsSupport.AllowSound);

			allowLightLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "AllowLightLabel",
				Localization.GetText ("AllowLightLabel"), ASLabelTypes.DefaultLeft);
			allowLightSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "AllowLightSwitch",
				false, settingsFieldBackColor, AllowLight_Toggled, NotificationsSupport.AllowLight);

			allowVibroLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "AllowVibroLabel",
				Localization.GetText ("AllowVibroLabel"), ASLabelTypes.DefaultLeft);
			allowVibroSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "AllowVibroSwitch",
				false, settingsFieldBackColor, AllowVibro_Toggled, NotificationsSupport.AllowVibro);

			AndroidSupport.ApplyLabelSettings (settingsPage, "IndicateOnlyUrgentLabel",
				Localization.GetText ("IndicateOnlyUrgentLabel"), ASLabelTypes.DefaultLeft);
			indicateOnlyUrgentSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "IndicateOnlyUrgentSwitch",
				false, settingsFieldBackColor, IndicateOnlyUrgent_Toggled,
				NotificationsSupport.IndicateOnlyUrgentNotifications);

			AndroidSupport.ApplyLabelSettings (settingsPage, "AppSettingsLabel",
				Localization.GetText ("AppSettingsLabel"), ASLabelTypes.HeaderLeft);

			AndroidSupport.ApplyLabelSettings (settingsPage, "KeepScreenOnLabel",
				Localization.GetText ("KeepScreenOnLabel"), ASLabelTypes.DefaultLeft);
			keepScreenOnSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "KeepScreenOnSwitch",
				false, settingsFieldBackColor, KeepScreenOnSwitch_Toggled, AndroidSupport.KeepScreenOn);

			allowLightLabel.IsVisible = allowLightSwitch.IsVisible = allowSoundLabel.IsVisible =
				allowSoundSwitch.IsVisible = allowVibroLabel.IsVisible = allowVibroSwitch.IsVisible =
				AndroidSupport.AreNotificationsConfigurable;

			AndroidSupport.ApplyLabelSettings (settingsPage, "CenterButtonLabel",
				Localization.GetText ("CenterButtonLabel"), ASLabelTypes.HeaderLeft);
			centerButtonFunction = AndroidSupport.ApplyButtonSettings (settingsPage, "CenterButtonFunction",
				NotificationsSupport.SpecialFunctionName, settingsFieldBackColor,
				SetSpecialFunction_Clicked, false);

			#endregion

			#region Настройки оповещений

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "SelectionLabel",
				Localization.GetText ("SelectionLabel"), ASLabelTypes.HeaderLeft);
			selectedNotification = AndroidSupport.ApplyButtonSettings (notSettingsPage, "SelectedNotification",
				"", notSettingsFieldBackColor, SelectNotification, false);

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "NameFieldLabel",
				Localization.GetText ("NameFieldLabel"), ASLabelTypes.DefaultLeft);
			nameField = AndroidSupport.ApplyEditorSettings (notSettingsPage, "NameField", notSettingsFieldBackColor,
				Keyboard.Text, Notification.MaxBeginningEndingLength, "", null, true);
			nameField.Placeholder = Localization.GetText ("NameFieldPlaceholder");

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "SettingsLabel",
				Localization.GetText ("SettingsLabel"), ASLabelTypes.HeaderLeft);

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "LinkFieldLabel",
				Localization.GetText ("LinkFieldLabel"), ASLabelTypes.DefaultLeft);
			linkFieldButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "LinkFieldButton",
				ASButtonDefaultTypes.Select, notSettingsFieldBackColor, SpecifyNotificationLink);

			occFieldLabel = AndroidSupport.ApplyLabelSettings (notSettingsPage, "OccFieldLabel", "",
				ASLabelTypes.DefaultLeft);
			occFieldLabel.TextType = TextType.Html;

			AndroidSupport.ApplyButtonSettings (notSettingsPage, "OccIncButton",
				ASButtonDefaultTypes.Increase, notSettingsFieldBackColor, OccurrenceChanged);
			AndroidSupport.ApplyButtonSettings (notSettingsPage, "OccDecButton",
				ASButtonDefaultTypes.Decrease, notSettingsFieldBackColor, OccurrenceChanged);
			currentOcc = 1;

			requestStepFieldLabel = AndroidSupport.ApplyLabelSettings (notSettingsPage, "RequestStepFieldLabel",
				"", ASLabelTypes.DefaultLeft);
			requestStepFieldLabel.TextType = TextType.Html;

			AndroidSupport.ApplyButtonSettings (notSettingsPage, "RequestStepIncButton",
				ASButtonDefaultTypes.Increase, notSettingsFieldBackColor, RequestStepChanged);
			AndroidSupport.ApplyButtonSettings (notSettingsPage, "RequestStepDecButton",
				ASButtonDefaultTypes.Decrease, notSettingsFieldBackColor, RequestStepChanged);
			AndroidSupport.ApplyButtonSettings (notSettingsPage, "RequestStepLongIncButton",
				ASButtonDefaultTypes.Create, notSettingsFieldBackColor, RequestStepChanged);
			currentFreq = NotificationsSet.DefaultUpdatingFrequency;

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "EnabledLabel",
				Localization.GetText ("EnabledLabel"), ASLabelTypes.DefaultLeft);
			enabledSwitch = AndroidSupport.ApplySwitchSettings (notSettingsPage, "EnabledSwitch",
				false, notSettingsFieldBackColor, null, false);

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "AvailabilityLabel",
				Localization.GetText ("AvailabilityLabel"), ASLabelTypes.DefaultLeft);
			notifyIfUnavailableSwitch = AndroidSupport.ApplySwitchSettings (notSettingsPage, "AvailabilitySwitch",
				false, notSettingsFieldBackColor, null, false);

			// Новые
			comparatorLabel = AndroidSupport.ApplyLabelSettings (notSettingsPage, "ComparatorLabel",
				Localization.GetText ("ComparatorLabelOff"), ASLabelTypes.DefaultLeft);
			comparatorSwitch = AndroidSupport.ApplySwitchSettings (notSettingsPage, "ComparatorSwitch",
				false, notSettingsFieldBackColor, ComparatorSwitch_Toggled, false);
			comparatorTypeButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ComparatorType",
				" ", notSettingsFieldBackColor, ComparatorTypeChanged, true);

			comparatorValueField = AndroidSupport.ApplyEditorSettings (notSettingsPage, "ComparatorValue",
				notSettingsFieldBackColor, Keyboard.Default, 10, "0", null, true);
			comparatorIncButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ComparatorValueIncButton",
				ASButtonDefaultTypes.Increase, notSettingsFieldBackColor, ComparatorValueChanged);
			comparatorDecButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ComparatorValueDecButton",
				ASButtonDefaultTypes.Decrease, notSettingsFieldBackColor, ComparatorValueChanged);
			comparatorLongButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ComparatorValueLongButton",
				ASButtonDefaultTypes.Create, notSettingsFieldBackColor, ComparatorValueChanged);

			ignoreMisfitsLabel = AndroidSupport.ApplyLabelSettings (notSettingsPage, "IgnoreMisfitsLabel",
				Localization.GetText ("IgnoreMisfitsLabel"), ASLabelTypes.DefaultLeft);
			ignoreMisfitsSwitch = AndroidSupport.ApplySwitchSettings (notSettingsPage, "IgnoreMisfitsSwitch",
				false, notSettingsFieldBackColor, null, false);

			// Инициализация полей
			ComparatorTypeChanged (null, null);
			ComparatorSwitch_Toggled (null, null);
			SelectNotification (null, null);

			#endregion

			#region Управление оповещениями

			applyButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ApplyButton",
				ASButtonDefaultTypes.Apply, notSettingsFieldBackColor, ApplyNotification);
			applyButton.HorizontalOptions = LayoutOptions.Fill;

			deleteButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "DeleteButton",
				ASButtonDefaultTypes.Delete, notSettingsFieldBackColor, DeleteNotification);

			AndroidSupport.ApplyButtonSettings (notSettingsPage, "ShareTemplateButton",
				ASButtonDefaultTypes.Share, notSettingsFieldBackColor, ShareTemplate);

			#endregion

			#region Страница "О программе"

			aboutLabel = AndroidSupport.ApplyLabelSettings (aboutPage, "AboutLabel",
				RDGenerics.AppAboutLabelText, ASLabelTypes.AppAbout);

			AndroidSupport.ApplyButtonSettings (aboutPage, "ManualsButton",
				Localization.GetDefaultText (LzDefaultTextValues.Control_ReferenceMaterials),
				aboutFieldBackColor, ReferenceButton_Click, false);
			AndroidSupport.ApplyButtonSettings (aboutPage, "HelpButton",
				Localization.GetDefaultText (LzDefaultTextValues.Control_HelpSupport),
				aboutFieldBackColor, HelpButton_Click, false);
			AndroidSupport.ApplyLabelSettings (aboutPage, "GenericSettingsLabel",
				Localization.GetDefaultText (LzDefaultTextValues.Control_GenericSettings),
				ASLabelTypes.HeaderLeft);

			UpdateNotButtons ();

			AndroidSupport.ApplyLabelSettings (aboutPage, "RestartTipLabel",
				Localization.GetDefaultText (LzDefaultTextValues.Message_RestartRequired),
				ASLabelTypes.Tip);

			AndroidSupport.ApplyLabelSettings (aboutPage, "LanguageLabel",
				Localization.GetDefaultText (LzDefaultTextValues.Control_InterfaceLanguage),
				ASLabelTypes.DefaultLeft);
			languageButton = AndroidSupport.ApplyButtonSettings (aboutPage, "LanguageSelector",
				Localization.LanguagesNames[(int)Localization.CurrentLanguage],
				aboutFieldBackColor, SelectLanguage_Clicked, false);

			AndroidSupport.ApplyLabelSettings (aboutPage, "FontSizeLabel",
				Localization.GetDefaultText (LzDefaultTextValues.Control_InterfaceFontSize),
				ASLabelTypes.DefaultLeft);
			AndroidSupport.ApplyButtonSettings (aboutPage, "FontSizeInc",
				ASButtonDefaultTypes.Increase, aboutFieldBackColor, FontSizeButton_Clicked);
			AndroidSupport.ApplyButtonSettings (aboutPage, "FontSizeDec",
				ASButtonDefaultTypes.Decrease, aboutFieldBackColor, FontSizeButton_Clicked);
			aboutFontSizeField = AndroidSupport.ApplyLabelSettings (aboutPage, "FontSizeField",
				" ", ASLabelTypes.DefaultCenter);

			AndroidSupport.ApplyLabelSettings (aboutPage, "HelpTextLabel",
				RDGenerics.GetEncoding (SupportedEncodings.UTF8).
				GetString ((byte[])RD_AAOW.Properties.Resources.ResourceManager.
				GetObject (Localization.GetHelpFilePath ())), ASLabelTypes.SmallLeft);

			FontSizeButton_Clicked (null, null);

			#endregion

			#region Страница журнала приложения

			mainLog = (Xamarin.Forms.ListView)logPage.FindByName ("MainLog");
			mainLog.BackgroundColor = logFieldBackColor;
			mainLog.HasUnevenRows = true;
			mainLog.ItemTapped += MainLog_ItemTapped;
			mainLog.ItemTemplate = new DataTemplate (typeof (NotificationView));
			mainLog.SelectionMode = ListViewSelectionMode.None;
			mainLog.SeparatorVisibility = SeparatorVisibility.None;
			mainLog.ItemAppearing += MainLog_ItemAppearing;
			((CarouselPage)MainPage).CurrentPageChanged += CurrentPageChanged;  // Пробуем исправить сброс прокрутки

			centerButton = AndroidSupport.ApplyButtonSettings (logPage, "CenterButton", " ",
				logFieldBackColor, CenterButton_Click, false);
			centerButton.Margin = centerButton.Padding = new Thickness (0);
			centerButton.FontSize += 6;

			scrollUpButton = AndroidSupport.ApplyButtonSettings (logPage, "ScrollUp", ASButtonDefaultTypes.Up,
				logFieldBackColor, ScrollUpButton_Click);
			scrollUpButton.Margin = scrollUpButton.Padding = new Thickness (0);

			scrollDownButton = AndroidSupport.ApplyButtonSettings (logPage, "ScrollDown", ASButtonDefaultTypes.Down,
				logFieldBackColor, ScrollDownButton_Click);
			scrollDownButton.Margin = scrollDownButton.Padding = new Thickness (0);

			// Режим чтения
			AndroidSupport.ApplyLabelSettings (settingsPage, "ReadModeLabel",
				Localization.GetText ("ReadModeLabel"), ASLabelTypes.DefaultLeft);
			nightModeSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "ReadModeSwitch",
				false, settingsFieldBackColor, NightModeSwitch_Toggled, NotificationsSupport.LogReadingMode);

			// Расположение новых записей в конце журнала
			AndroidSupport.ApplyLabelSettings (settingsPage, "NewsAtTheEndLabel",
				Localization.GetText ("NewsAtTheEndLabel"), ASLabelTypes.DefaultLeft);
			newsAtTheEndSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "NewsAtTheEndSwitch",
				false, settingsFieldBackColor, NewsAtTheEndSwitch_Toggled, NotificationsSupport.LogNewsItemsAtTheEnd);

			#endregion

			#region Прочие настройки

			AndroidSupport.ApplyLabelSettings (settingsPage, "LogSettingsLabel",
				Localization.GetText ("LogSettingsLabel"), ASLabelTypes.HeaderLeft);

			// Режим чтения
			NightModeSwitch_Toggled (null, null);

			// Размер шрифта
			fontSizeFieldLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "FontSizeFieldLabel",
				"", ASLabelTypes.DefaultLeft);
			fontSizeFieldLabel.TextType = TextType.Html;

			AndroidSupport.ApplyButtonSettings (settingsPage, "FontSizeIncButton",
				ASButtonDefaultTypes.Increase, settingsFieldBackColor, FontSizeChanged);
			AndroidSupport.ApplyButtonSettings (settingsPage, "FontSizeDecButton",
				ASButtonDefaultTypes.Decrease, settingsFieldBackColor, FontSizeChanged);

			FontSizeChanged (null, null);

			AndroidSupport.ApplyButtonSettings (settingsPage, "SaveQuietSound",
				Localization.GetText ("SaveQuietSound"), settingsFieldBackColor,
				SaveQuietSound_Clicked, false);

			#endregion

			// Запуск цикла обратной связи (без ожидания)
			FinishBackgroundRequest ();

			// Принятие соглашений
			ShowStartupTips (Huawei);
			}

		// Исправление для сброса текущей позиции журнала
		private async void CurrentPageChanged (object sender, EventArgs e)
			{
			if (((CarouselPage)MainPage).Children.IndexOf (((CarouselPage)MainPage).CurrentPage) != 0)
				return;

			needsScroll = true;
			await ScrollMainLog (newsAtTheEndSwitch.IsToggled, -1);
			}

		// Цикл обратной связи для загрузки текущего журнала, если фоновая служба не успела завершить работу
		private async Task<bool> FinishBackgroundRequest ()
			{
			// Ожидание завершения операции
			SetLogState (false);

			UpdateLogButton (true, true);
			if (NotificationsSupport.BackgroundRequestInProgress)
				{
				CenterButton_Click (null, null);

				masterLog = new List<MainLogItem> (NotificationsSupport.GetMasterLog (false));
				UpdateLog ();
				}

			await Task<bool>.Run (WaitForFinishingRequest);

			// Перезапрос журнала
			if (masterLog != null)
				masterLog.Clear ();
			masterLog = new List<MainLogItem> (NotificationsSupport.GetMasterLog (true));

			needsScroll = true;
			UpdateLog ();

			SetLogState (true);
			return true;
			}

		// Отвязанный от текущего контекста нагрузочный процесс с запросом
		private bool WaitForFinishingRequest ()
			{
			while (NotificationsSupport.BackgroundRequestInProgress && AndroidSupport.AppIsRunning)
				Thread.Sleep (1000);

			return true;
			}

		// Метод отображает подсказки при первом запуске
		private async void ShowStartupTips (bool Huawei)
			{
			// Контроль XPUN
			await AndroidSupport.XPUNLoop (Huawei);

			// Требование принятия Политики
			if (!NotificationsSupport.GetTipState (NSTipTypes.PolicyTip))
				{
				await AndroidSupport.PolicyLoop ();
				NotificationsSupport.SetTipState (NSTipTypes.PolicyTip);
				}

			// Подсказки
			if (!NotificationsSupport.GetTipState (NSTipTypes.StartupTips))
				{
				await AndroidSupport.ShowMessage (Localization.GetText ("Tip01"),
					Localization.GetDefaultText (LzDefaultTextValues.Button_Next));

				await AndroidSupport.ShowMessage (Localization.GetText ("Tip02"),
					Localization.GetDefaultText (LzDefaultTextValues.Button_Next));

				await AndroidSupport.ShowMessage (Localization.GetText ("Tip03_1"),
					AndroidSupport.AreNotificationsConfigurable ?
					Localization.GetDefaultText (LzDefaultTextValues.Button_OK) :
					Localization.GetDefaultText (LzDefaultTextValues.Button_Next));

				if (!AndroidSupport.AreNotificationsConfigurable)
					await AndroidSupport.ShowMessage (Localization.GetText ("Tip03_2"),
						Localization.GetDefaultText (LzDefaultTextValues.Button_OK));

				NotificationsSupport.SetTipState (NSTipTypes.StartupTips);
				NotificationsSupport.SetTipState (NSTipTypes.FrequencyTip);
				}

			// Нежелательно дублировать
			if (!NotificationsSupport.GetTipState (NSTipTypes.FrequencyTip))
				{
				await AndroidSupport.ShowMessage (Localization.GetText ("Tip04_21"),
					Localization.GetDefaultText (LzDefaultTextValues.Button_OK));

				NotificationsSupport.SetTipState (NSTipTypes.FrequencyTip);
				}

			if (AndroidSupport.AllowFontSizeTip)
				{
				await AndroidSupport.ShowMessage (
					Localization.GetDefaultText (LzDefaultTextValues.Message_FontSizeAvailable),
					Localization.GetDefaultText (LzDefaultTextValues.Button_OK));
				}
			}

		// Метод отображает остальные подсказки
		private async Task<bool> ShowTips (NSTipTypes Type)
			{
			// Подсказки
			await AndroidSupport.ShowMessage (Localization.GetText ("Tip04_" + ((int)Type).ToString ()),
				Localization.GetDefaultText (LzDefaultTextValues.Button_OK));

			NotificationsSupport.SetTipState (Type);
			return true;
			}

		/// <summary>
		/// Метод выполняет возврат на страницу содержания
		/// </summary>
		public void CallHeadersPage ()
			{
			((CarouselPage)MainPage).CurrentPage = logPage;
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
			NotificationsSupport.SetMasterLog (masterLog);
			AndroidSupport.AppIsRunning = false;

			// На случай, если срочное оповещение получено в ручном режиме
			ProgramDescription.NSet.HasUrgentNotifications = false;
			}

		/// <summary>
		/// Возврат в интерфейс
		/// </summary>
		protected override void OnResume ()
			{
			// Запуск цикла обратной связи (без ожидания, на случай, если приложение было свёрнуто, но не закрыто,
			// а во время ожидания имели место обновления журнала)
			AndroidSupport.AppIsRunning = true;
			FinishBackgroundRequest ();
			}

		#endregion

		#region Журнал

		// Запрос всех новостей
		private async Task<string> GetNotification ()
			{
			// Оболочка с включённой в неё паузой (иначе блокируется интерфейсный поток)
			if (getNotificationIndex < 0)
				{
				Thread.Sleep ((int)ProgramDescription.MasterFrameLength * 2);
				return await ProgramDescription.NSet.GetNextNotification (-1);
				}
			else
				{
				return await ProgramDescription.NSet.GetNextNotification (getNotificationIndex);
				}
			}

		private async void AllNewsItems ()
			{
			// Проверка
			if (!await AndroidSupport.ShowMessage (Localization.GetText ("AllNewsRequest"),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Yes),
				Localization.GetDefaultText (LzDefaultTextValues.Button_No)))
				return;

			// Блокировка
			SetLogState (false);
			AndroidSupport.ShowBalloon (Localization.GetText ("RequestAllStarted"), true);

			// Запрос
			AndroidSupport.StopRequested = false;       // Разблокировка метода GetHTML
			ProgramDescription.NSet.ResetTimer (false); // Без сброса текстов
			string newText = "";

			// Опрос с защитой от закрытия приложения до завершения опроса
			getNotificationIndex = -1;  // Порядковый опрос
			while (AndroidSupport.AppIsRunning && ((newText = await Task.Run<string> (GetNotification)) !=
				NotificationsSet.NoNewsSign))
				if (newText != "")
					{
					// Запись в журнал
					AddTextToLog (newText);
					needsScroll = true;
					UpdateLog ();
					}

			// Разблокировка
			SetLogState (true);
			AndroidSupport.ShowBalloon (Localization.GetText ("RequestCompleted"), true);

			if (!NotificationsSupport.GetTipState (NSTipTypes.MainLogClickMenuTip))
				await ShowTips (NSTipTypes.MainLogClickMenuTip);
			}

		// Принудительное обновление лога
		private void UpdateLog ()
			{
			mainLog.ItemsSource = null;
			mainLog.ItemsSource = masterLog;
			}

		// Промотка журнала к нужной позиции
		private async void MainLog_ItemAppearing (object sender, ItemVisibilityEventArgs e)
			{
			await ScrollMainLog (newsAtTheEndSwitch.IsToggled, e.ItemIndex);
			}

		private async Task<bool> ScrollMainLog (bool ToTheEnd, int VisibleItem)
			{
			// Контроль
			if (masterLog == null)
				return false;

			if ((masterLog.Count < 1) || !needsScroll)
				return false;

			// Искусственная задержка
			await Task.Delay (100);

			// Промотка с повторением до достижения нужного участка
			if (ToTheEnd)
				{
				if ((VisibleItem < 0) || (VisibleItem > masterLog.Count - 3))
					needsScroll = false;

				mainLog.ScrollTo (masterLog[masterLog.Count - 1], ScrollToPosition.MakeVisible, false);
				}
			else
				{
				if ((VisibleItem < 0) || (VisibleItem < 2))
					needsScroll = false;

				mainLog.ScrollTo (masterLog[0], ScrollToPosition.MakeVisible, false);
				}

			return true;
			}

		// Обновление формы кнопки журнала
		private void UpdateLogButton (bool Requesting, bool FinishingBackgroundRequest)
			{
			bool red = Requesting && FinishingBackgroundRequest;
			bool yellow = Requesting && !FinishingBackgroundRequest;
			bool green = (NotificationsSupport.SpecialFunctionNumber > 0) && !Requesting &&
				!FinishingBackgroundRequest;
			bool dark = nightModeSwitch.IsToggled;

			if (red || yellow || green)
				{
				centerButton.Text = (red ? semaphoreOn : semaphoreOff) + (yellow ? semaphoreOn : semaphoreOff) +
					(green ? semaphoreOn : semaphoreOff);

				if (red)
					centerButton.TextColor = Color.FromHex (dark ? "#FF4040" : "#D00000");
				else if (yellow)
					centerButton.TextColor = Color.FromHex (dark ? "#FFFF40" : "#D0D000");
				else
					centerButton.TextColor = Color.FromHex (dark ? "#40FF40" : "#00D000");
				}
			else
				{
				centerButton.Text = "   ";
				}
			}

		// Выбор оповещения для перехода или share
		private async void MainLog_ItemTapped (object sender, ItemTappedEventArgs e)
			{
			// Контроль
			MainLogItem notItem = (MainLogItem)e.Item;
			if (!centerButtonEnabled || (notItem.StringForSaving == ""))  // Признак разделителя
				return;

			// Сброс состояния
			UpdateLogButton (false, false);

			// Извлечение ссылки и номера оповещения
			string notLink = "";
			int notNumber = -1;
			if (notItem.Header.Contains (GMJ.SourceName))
				{
				notLink = GMJ.SourceRedirectLink;

				int l, r;
				if (((l = notItem.Header.IndexOf (GMJ.NumberStringBeginning)) >= 0) &&
					((r = notItem.Header.IndexOf (GMJ.NumberStringEnding, l)) >= 0))
					{
					l += GMJ.NumberStringBeginning.Length;
					notLink += ("/" + notItem.Header.Substring (l, r - l));
					}
				}
			else
				{
				for (notNumber = 0; notNumber < ProgramDescription.NSet.Notifications.Count; notNumber++)
					if (notItem.Header.StartsWith (NotificationsSet.HeaderBeginning +
						ProgramDescription.NSet.Notifications[notNumber].Name + NotificationsSet.HeaderMiddle))
						{
						notLink = ProgramDescription.NSet.Notifications[notNumber].Link;
						break;
						}
				}

			// Формирование меню
			int variant = 0, menuItem;
			if (tapMenuItems.Count < 1)
				{
				tapMenuItems.Add (new List<string> {
					"☍\t" + Localization.GetText ("ShareOption"),
					"❏\t" + Localization.GetText ("CopyOption"),
					Localization.GetText ("OtherOption")
					});
				tapMenuItems.Add (new List<string> {
					"▷\t" + Localization.GetText ("GoToOption"),
					"☍\t" + Localization.GetText ("ShareOption"),
					"❏\t" + Localization.GetText ("CopyOption"),
					Localization.GetText ("OtherOption")
					});
				tapMenuItems.Add (new List<string> {
					"✕\t" + Localization.GetText ("RemoveOption")
					});
				tapMenuItems.Add (new List<string> {
					"▷\t" + Localization.GetText ("GoToOption"),
					"☍\t" + Localization.GetText ("ShareOption"),
					"❏\t" + Localization.GetText ("CopyOption"),
					"↺\t" + Localization.GetText ("RequestAgainOption"),
					"✎\t" + Localization.GetText ("SetupOption"),
					Localization.GetText ("OtherOption")
					});
				tapMenuItems.Add (new List<string> {
					"✕\t" + Localization.GetText ("RemoveOption"),
					"✂\t" + Localization.GetText ("DisableOption")
					});
				}

			// Запрос варианта использования
			if ((notNumber < 0) || (notNumber >= ProgramDescription.NSet.Notifications.Count))
				{
				menuItem = (string.IsNullOrWhiteSpace (notLink) ? 0 : 1);
				menuItem = await AndroidSupport.ShowList (Localization.GetText ("SelectOption"),
					Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel),
					tapMenuItems[menuItem]);

				if (menuItem < 0)
					return;

				variant = menuItem + 10;
				if (string.IsNullOrWhiteSpace (notLink))
					variant++;

				// Контроль второго набора
				if (variant > 12)
					{
					menuItem = await AndroidSupport.ShowList (Localization.GetText ("SelectOption"),
						Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel), tapMenuItems[2]);
					if (menuItem < 0)
						return;

					variant += menuItem;
					}
				}
			else
				{
				menuItem = await AndroidSupport.ShowList (Localization.GetText ("SelectOption"),
					Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel), tapMenuItems[3]);
				if (menuItem < 0)
					return;

				variant = menuItem;

				// Контроль второго набора
				if (variant > 4)
					{
					menuItem = await AndroidSupport.ShowList (Localization.GetText ("SelectOption"),
						Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel), tapMenuItems[4]);
					if (menuItem < 0)
						return;

					variant += menuItem;
					}
				}

			// Обработка (неподходящие варианты будут отброшены)
			switch (variant)
				{
				// Переход по ссылке
				case 0:
				case 10:
					if (!NotificationsSupport.GetTipState (NSTipTypes.GoToButton))
						await ShowTips (NSTipTypes.GoToButton);

					try
						{
						await Launcher.OpenAsync (notLink);
						}
					catch
						{
						AndroidSupport.ShowBalloon
							(Localization.GetDefaultText (LzDefaultTextValues.Message_BrowserNotAvailable), true);
						}
					break;

				// Поделиться
				case 1:
				case 11:
					if (!NotificationsSupport.GetTipState (NSTipTypes.ShareButton))
						await ShowTips (NSTipTypes.ShareButton);

					await Share.RequestAsync ((notItem.Header + Localization.RNRN + notItem.Text +
						Localization.RNRN + notLink).Replace ("\r", ""), ProgramDescription.AssemblyVisibleName);
					break;

				// Скопировать в буфер обмена
				case 2:
				case 12:
					try
						{
						await Clipboard.SetTextAsync ((notItem.Header + Localization.RNRN + notItem.Text +
							Localization.RNRN + notLink).Replace ("\r", ""));
						AndroidSupport.ShowBalloon (Localization.GetText ("CopyMessage"), false);

						}
					catch { }
					break;

				// Повторный опрос
				case 3:
					// Блокировка
					SetLogState (false);
					AndroidSupport.ShowBalloon (Localization.GetText ("RequestStarted"), true);

					// Запрос
					AndroidSupport.StopRequested = false;           // Разблокировка метода GetHTML
					ProgramDescription.NSet.ResetTimer (false);     // Без сброса текстов
					string newText = "";

					// Опрос с защитой от закрытия приложения до завершения опроса
					getNotificationIndex = notNumber;
					if (AndroidSupport.AppIsRunning && ((newText = await Task.Run<string> (GetNotification)) != ""))
						{
						// Запись в журнал
						AddTextToLog (newText);
						UpdateLog ();
						}

					// Разблокировка
					SetLogState (true);
					AndroidSupport.ShowBalloon (Localization.GetText ("RequestCompleted"), true);
					break;

				// Настройка оповещения
				case 4:
					currentNotification = notNumber;
					SelectNotification (null, null);
					((CarouselPage)MainPage).CurrentPage = ((CarouselPage)MainPage).Children[notSettingsTab];
					break;

				// Удаление из журнала
				case 5:
				case 13:
					masterLog.RemoveAt (e.ItemIndex);
					UpdateLog ();
					break;

				// Отключение оповещения
				case 6:
					currentNotification = notNumber;
					SelectNotification (null, null);
					enabledSwitch.IsToggled = false;
					ApplyNotification (null, null);
					break;
				}

			// Завершено
			}

		// Блокировка / разблокировка кнопок
		private void SetLogState (bool State)
			{
			// Переключение состояния кнопок и свичей
			centerButtonEnabled = State;

			settingsPage.IsEnabled = notSettingsPage.IsEnabled = aboutPage.IsEnabled = State;
			if (!State)
				{
				settingsPage.BackgroundColor = notSettingsPage.BackgroundColor =
					aboutPage.BackgroundColor = solutionLockedBackColor;
				}
			else
				{
				settingsPage.BackgroundColor = settingsMasterBackColor;
				notSettingsPage.BackgroundColor = notSettingsMasterBackColor;
				aboutPage.BackgroundColor = aboutMasterBackColor;
				}

			// Обновление статуса
			UpdateLogButton (!State, false);
			}

		// Добавление текста в журнал
		private void AddTextToLog (string Text)
			{
			if (newsAtTheEndSwitch.IsToggled)
				{
				masterLog.Add (new MainLogItem (Text));

				// Удаление верхних строк
				while (masterLog.Count >= ProgramDescription.MasterLogMaxItems)
					masterLog.RemoveAt (0);
				}
			else
				{
				masterLog.Insert (0, new MainLogItem (Text));

				// Удаление нижних строк (здесь требуется, т.к. не выполняется обрезка свойством .MainLog)
				while (masterLog.Count >= ProgramDescription.MasterLogMaxItems)
					masterLog.RemoveAt (masterLog.Count - 1);
				}
			}

		// Действия средней кнопки журнала
		private void CenterButton_Click (object sender, EventArgs e)
			{
			if (!centerButtonEnabled)
				{
				AndroidSupport.ShowBalloon (Localization.GetText ("BackgroundRequestInProgress"), true);
				return;
				}

			switch (NotificationsSupport.SpecialFunctionNumber)
				{
				// Без действия
				case 0:
					break;

				// Опрос всех новостей
				case 1:
					AllNewsItems ();
					break;

				// Вызов дополнительных функций
				default:
					GetGMJ ();
					break;
				}
			}

		private async void GetGMJ ()
			{
			// Блокировка на время опроса
			SetLogState (false);
			AndroidSupport.ShowBalloon (Localization.GetText ("RequestStarted"), false);

			// Запуск и разбор
			AndroidSupport.StopRequested = false; // Разблокировка метода GetHTML
			string newText = "";

			for (int i = 0; i < 3; i++)     // Минимизация возможных попаданий в пропуски
				{
				newText = await Task.Run<string> (GMJ.GetRandomGMJ);

				if (!newText.Contains (GMJ.SourceNoReturnPattern))
					break;
				}

			if (newText == "")
				{
				AndroidSupport.ShowBalloon (Localization.GetText ("GMJRequestFailed"), true);
				}
			else if (newText.Contains (GMJ.SourceNoReturnPattern))
				{
				AndroidSupport.ShowBalloon (newText, true);
				}
			else
				{
				AddTextToLog (newText);
				needsScroll = true;
				UpdateLog ();
				}

			// Разблокировка
			SetLogState (true);
			if (!NotificationsSupport.GetTipState (NSTipTypes.MainLogClickMenuTip))
				await ShowTips (NSTipTypes.MainLogClickMenuTip);
			}

		// Ручная прокрутка
		private async void ScrollUpButton_Click (object sender, EventArgs e)
			{
			needsScroll = true;
			await ScrollMainLog (false, -1);
			}

		private async void ScrollDownButton_Click (object sender, EventArgs e)
			{
			needsScroll = true;
			await ScrollMainLog (true, -1);
			}

		#endregion

		#region Основные настройки

		// Включение / выключение фиксации экрана
		private async void KeepScreenOnSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NSTipTypes.KeepScreenOnTip))
				await ShowTips (NSTipTypes.KeepScreenOnTip);

			AndroidSupport.KeepScreenOn = keepScreenOnSwitch.IsToggled;
			}

		// Индикация только срочных уведомлений
		private async void IndicateOnlyUrgent_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NSTipTypes.OnlyUrgent))
				await ShowTips (NSTipTypes.OnlyUrgent);

			NotificationsSupport.IndicateOnlyUrgentNotifications = indicateOnlyUrgentSwitch.IsToggled;
			}

		// Включение / выключение добавления новостей с конца журнала
		private void NewsAtTheEndSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			// Обновление журнала
			if (e != null)
				NotificationsSupport.LogNewsItemsAtTheEnd = newsAtTheEndSwitch.IsToggled;

			UpdateLogButton (false, false);
			}

		// Метод запускает мастер оповещений
		private async void StartNotificationsWizard (object sender, EventArgs e)
			{
			// Подсказки
			notWizardButton.IsEnabled = false;

			// Запрос варианта использования
			if (wizardMenuItems.Count < 1)
				{
				wizardMenuItems = new List<string> {
					Localization.GetText ("NotificationsWizard"),
					Localization.GetText ("TemplateList"),
					Localization.GetText ("CopyNotification"),
					Localization.GetText ("TemplateClipboard"),
					Localization.GetText ("TemplateFile"),
				};
				}

			int res = await AndroidSupport.ShowList (Localization.GetText ("TemplateSelect"),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel), wizardMenuItems);

			// Обработка
			switch (res)
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
						AndroidSupport.ShowBalloon (Localization.GetText ("UpdatingTemplates"), true);

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

					res = await AndroidSupport.ShowList (Localization.GetText ("SelectTemplate"),
						Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel), templatesNames);

					// Установка результата
					uint templateNumber = 0;
					if (res >= 0)
						{
						templateNumber = (uint)res;
						}
					else
						{
						notWizardButton.IsEnabled = true;
						return;
						}

					// Проверка
					if (ProgramDescription.NSet.NotificationsTemplates.IsTemplateIncomplete (templateNumber))
						await AndroidSupport.ShowMessage (Localization.GetText ("CurlyTemplate"),
							Localization.GetDefaultText (LzDefaultTextValues.Button_OK));

					// Заполнение
					nameField.Text = ProgramDescription.NSet.NotificationsTemplates.GetName (templateNumber);
					linkField = ProgramDescription.NSet.NotificationsTemplates.GetLink (templateNumber);
					beginningField = ProgramDescription.NSet.NotificationsTemplates.GetBeginning (templateNumber);
					endingField = ProgramDescription.NSet.NotificationsTemplates.GetEnding (templateNumber);
					currentOcc = ProgramDescription.NSet.NotificationsTemplates.GetOccurrenceNumber (templateNumber);
					currentFreq = NotificationsSet.DefaultUpdatingFrequency;

					break;

				// Разбор переданного шаблона
				case 3:
					// Запрос из буфера обмена
					string text = "";
					try
						{
						text = await Clipboard.GetTextAsync ();
						}
					catch { }

					if ((text == null) || (text == ""))
						{
						await AndroidSupport.ShowMessage (Localization.GetText ("NoTemplateInClipboard"),
							Localization.GetDefaultText (LzDefaultTextValues.Button_OK));
						notWizardButton.IsEnabled = true;
						return;
						}

					// Разбор
					string[] values = text.Split (NotificationsTemplatesProvider.ClipboardTemplateSplitter,
						StringSplitOptions.RemoveEmptyEntries);
					if (values.Length != 5)
						{
						await AndroidSupport.ShowMessage (Localization.GetText ("NoTemplateInClipboard"),
							Localization.GetDefaultText (LzDefaultTextValues.Button_OK));
						notWizardButton.IsEnabled = true;
						return;
						}

					// Заполнение
					nameField.Text = values[0];
					linkField = values[1];
					beginningField = values[2];
					endingField = values[3];
					try
						{
						currentOcc = uint.Parse (values[4]);
						}
					catch
						{
						currentOcc = 1;
						}
					currentFreq = NotificationsSet.DefaultUpdatingFrequency;

					break;

				// Создание копированием
				case 2:
					// Запрос списка оповещений
					List<string> list = new List<string> ();
					foreach (Notification element in ProgramDescription.NSet.Notifications)
						list.Add (element.Name);

					res = await AndroidSupport.ShowList (Localization.GetText ("SelectNotification"),
						Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel), list);

					// Создание псевдокопии
					if (res >= 0)
						{
						currentNotification = res;
						SelectNotification (null, null);

						nameField.Text = "*" + nameField.Text;
						}

					list.Clear ();
					break;

				// Загрузка из файла
				case 4:
					// Чтение
					notWizardButton.IsEnabled = true;

					if (!await AndroidSupport.ShowMessage (Localization.GetText ("LoadingWarning"),
						Localization.GetDefaultText (LzDefaultTextValues.Button_Yes),
						Localization.GetDefaultText (LzDefaultTextValues.Button_No)))
						return;

					string settings = await AndroidSupport.LoadFromFile (SupportedEncodings.Unicode16);
					ProgramDescription.NSet.SetSettingsList (settings);

					// Сброс состояния
					currentNotification = 0;
					SelectNotification (null, null);
					return;
				}

			// Обновление
			OccurrenceChanged (null, null);
			RequestStepChanged (null, null);

			// Создание уведомления
			enabledSwitch.IsToggled = true;
			notifyIfUnavailableSwitch.IsToggled = false;
			comparatorSwitch.IsToggled = false;

			if (await UpdateItem (-1))
				{
				currentNotification = ProgramDescription.NSet.Notifications.Count - 1;
				SelectNotification (null, null);

				AndroidSupport.ShowBalloon (Localization.GetText ("AddAsNewMessage") + nameField.Text, false);
				}

			// Переход к дополнительным опциям
			notWizardButton.IsEnabled = true;
			((CarouselPage)MainPage).CurrentPage = ((CarouselPage)MainPage).Children[notSettingsTab];
			}

		// Вызов помощника по созданию оповещений
		private async Task<bool> NotificationsWizard ()
			{
			// Шаг запроса ссылки
			NotConfiguration cfg;

			cfg.SourceLink = await AndroidSupport.ShowInput (ProgramDescription.AssemblyVisibleName,
				Localization.GetText ("WizardStep1"),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Next),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel),
				Notification.MaxLinkLength, Keyboard.Url, "", Localization.GetText ("LinkFieldPlaceholder"));

			if (string.IsNullOrWhiteSpace (cfg.SourceLink))
				return false;

			// Шаг запроса ключевого слова
			string keyword = await AndroidSupport.ShowInput (ProgramDescription.AssemblyVisibleName,
				Localization.GetText ("WizardStep2"),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Next),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel),
				Notification.MaxBeginningEndingLength, Keyboard.Default);

			if (string.IsNullOrWhiteSpace (keyword))
				return false;

			// Запуск
			AndroidSupport.ShowBalloon (Localization.GetText ("WizardSearch1"), true);

			string[] delim = await Notification.FindDelimiters (cfg.SourceLink, keyword);
			if (delim == null)
				{
				await AndroidSupport.ShowMessage (Localization.GetText ("WizardFailure"),
					Localization.GetDefaultText (LzDefaultTextValues.Button_OK));
				return false;
				}

			// Попытка запроса
			for (cfg.OccurrenceNumber = 1; cfg.OccurrenceNumber <= 3; cfg.OccurrenceNumber++)
				{
				AndroidSupport.ShowBalloon (Localization.GetText ("WizardSearch2"), true);

				cfg.NotificationName = "Test";
				cfg.WatchAreaBeginningSign = delim[0];
				cfg.WatchAreaEndingSign = delim[1];
				cfg.UpdatingFrequency = 1;
				cfg.ComparisonType = NotComparatorTypes.Disabled;
				cfg.ComparisonString = "";
				cfg.IgnoreComparisonMisfits = cfg.NotifyWhenUnavailable = false;

				Notification not = new Notification (cfg);
				if (!await not.Update ())
					{
					await AndroidSupport.ShowMessage (Localization.GetText ("WizardFailure"),
						Localization.GetDefaultText (LzDefaultTextValues.Button_OK));
					return false;
					}

				// Получен текст, проверка
				string text = not.CurrentText;
				if (text.Length > 300)
					text = text.Substring (0, 297) + "...";

				bool notLastStep = (cfg.OccurrenceNumber < 3);
				if (await AndroidSupport.ShowMessage (Localization.GetText (notLastStep ?
					"WizardStep3" : "WizardStep4") + Localization.RNRN + "~".PadRight (10, '~') +
					Localization.RNRN + text,
					Localization.GetDefaultText (LzDefaultTextValues.Button_Next),
					Localization.GetDefaultText (notLastStep ?
					LzDefaultTextValues.Button_Retry : LzDefaultTextValues.Button_Cancel)))
					{
					break;
					}
				else
					{
					if (notLastStep)
						continue;
					else
						return false;
					}
				}

			// Завершено, запрос названия
			string name = await AndroidSupport.ShowInput (ProgramDescription.AssemblyVisibleName,
				Localization.GetText ("WizardStep5"),
				Localization.GetDefaultText (LzDefaultTextValues.Button_OK),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel),
				Notification.MaxBeginningEndingLength, Keyboard.Text);

			if (string.IsNullOrWhiteSpace (name))
				return false;

			// Добавление оповещения
			nameField.Text = name;
			linkField = cfg.SourceLink;
			beginningField = delim[0];
			endingField = delim[1];
			currentOcc = cfg.OccurrenceNumber;
			currentFreq = NotificationsSet.DefaultUpdatingFrequency;

			return true;
			}

		// Включение / выключение службы
		private async void AllowStart_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NSTipTypes.ServiceLaunchTip))
				await ShowTips (NSTipTypes.ServiceLaunchTip);

			AndroidSupport.AllowServiceToStart = allowStart.IsToggled;
			}

		// Включение / выключение вариантов индикации
		private async void AllowSound_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NSTipTypes.IndicationTip))
				await ShowTips (NSTipTypes.IndicationTip);

			NotificationsSupport.AllowSound = allowSoundSwitch.IsToggled;
			}

		private async void AllowLight_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NSTipTypes.IndicationTip))
				await ShowTips (NSTipTypes.IndicationTip);

			NotificationsSupport.AllowLight = allowLightSwitch.IsToggled;
			}

		private async void AllowVibro_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NSTipTypes.IndicationTip))
				await ShowTips (NSTipTypes.IndicationTip);

			NotificationsSupport.AllowVibro = allowVibroSwitch.IsToggled;
			}

		// Включение / выключение режима чтения для лога
		private void NightModeSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			if (e != null)
				NotificationsSupport.LogReadingMode = nightModeSwitch.IsToggled;

			if (nightModeSwitch.IsToggled)
				{
				logPage.BackgroundColor = mainLog.BackgroundColor = centerButton.BackgroundColor =
					scrollUpButton.BackgroundColor = scrollDownButton.BackgroundColor = logReadModeColor;
				NotificationsSupport.LogFontColor = logMasterBackColor;
				}
			else
				{
				logPage.BackgroundColor = mainLog.BackgroundColor = centerButton.BackgroundColor =
					scrollUpButton.BackgroundColor = scrollDownButton.BackgroundColor = logMasterBackColor;
				NotificationsSupport.LogFontColor = logReadModeColor;
				}
			scrollUpButton.TextColor = scrollDownButton.TextColor = NotificationView.CurrentAntiBackColor;

			// Принудительное обновление (только не при старте)
			if (e != null)
				{
				needsScroll = true;
				UpdateLog ();
				}

			// Цепляет кнопку журнала
			UpdateLogButton (false, false);
			}

		// Изменение размера шрифта лога
		private void FontSizeChanged (object sender, EventArgs e)
			{
			uint fontSize = NotificationsSupport.LogFontSize;

			if (e != null)
				{
				Xamarin.Forms.Button b = (Xamarin.Forms.Button)sender;
				if (AndroidSupport.IsNameDefault (b.Text, ASButtonDefaultTypes.Increase) &&
					(fontSize < AndroidSupport.MaxFontSize))
					fontSize++;
				else if (AndroidSupport.IsNameDefault (b.Text, ASButtonDefaultTypes.Decrease) &&
					(fontSize > AndroidSupport.MinFontSize))
					fontSize--;

				NotificationsSupport.LogFontSize = fontSize;
				}

			// Принудительное обновление
			fontSizeFieldLabel.Text = string.Format (Localization.GetText ("FontSizeLabel"), fontSize.ToString ());

			if (e != null)
				{
				needsScroll = true;
				UpdateLog ();
				}
			}

		// Выбор языка приложения
		private async void SetSpecialFunction_Clicked (object sender, EventArgs e)
			{
			// Список опций
			if (specialOptions.Count < 1)
				{
				specialOptions.Add (Localization.GetText ("SpecialFunction_Nothing"));
				specialOptions.Add (Localization.GetText ("SpecialFunction_AllNews"));

				string[] sources = GMJ.SourceNames;
				if (Localization.IsCurrentLanguageRuRu)
					{
					for (int i = 0; i < sources.Length; i++)
						specialOptions.Add (Localization.GetText ("SpecialFunction_Get") + sources[i]);
					}
				}

			// Запрос
			int res = await AndroidSupport.ShowList (Localization.GetText ("SelectSpecialFunction"),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel), specialOptions);

			// Сохранение
			if (res < 0)
				return;

			NotificationsSupport.SpecialFunctionNumber = (uint)res;
			NotificationsSupport.SpecialFunctionName = centerButtonFunction.Text = specialOptions[res];
			UpdateLogButton (false, false);

			if (res > 1)
				GMJ.SourceNumber = (uint)(res - 2);
			}

		// Метод сохраняет тихий звук
		private async void SaveQuietSound_Clicked (object sender, EventArgs e)
			{
			await AndroidSupport.SaveToFile ("Silence.mp3", RD_AAOW.Properties.Resources.MuteSound);
			}

		#endregion

		#region О приложении

		// Выбор языка приложения
		private async void SelectLanguage_Clicked (object sender, EventArgs e)
			{
			languageButton.Text = await AndroidSupport.CallLanguageSelector ();
			}

		// Вызов справочных материалов
		private async void ReferenceButton_Click (object sender, EventArgs e)
			{
			await AndroidSupport.CallHelpMaterials (HelpMaterialsSets.ReferenceMaterials);
			}

		private async void HelpButton_Click (object sender, EventArgs e)
			{
			await AndroidSupport.CallHelpMaterials (HelpMaterialsSets.HelpAndSupport);
			}

		// Изменение размера шрифта интерфейса
		private void FontSizeButton_Clicked (object sender, EventArgs e)
			{
			if (sender != null)
				{
				Xamarin.Forms.Button b = (Xamarin.Forms.Button)sender;
				if (AndroidSupport.IsNameDefault (b.Text, ASButtonDefaultTypes.Increase))
					AndroidSupport.MasterFontSize += 0.5;
				else if (AndroidSupport.IsNameDefault (b.Text, ASButtonDefaultTypes.Decrease))
					AndroidSupport.MasterFontSize -= 0.5;
				}

			aboutFontSizeField.Text = AndroidSupport.MasterFontSize.ToString ("F1");
			aboutFontSizeField.FontSize = AndroidSupport.MasterFontSize;
			}

		#endregion

		#region Настройка оповещений

		// Выбор оповещения
		private async void SelectNotification (object sender, EventArgs e)
			{
			// Подсказки
			if ((e != null) && !NotificationsSupport.GetTipState (NSTipTypes.CurrentNotButton))
				await ShowTips (NSTipTypes.CurrentNotButton);

			// Запрос списка оповещений
			List<string> list = new List<string> ();
			foreach (Notification element in ProgramDescription.NSet.Notifications)
				list.Add (element.Name);

			int res = currentNotification;
			if (e != null)
				res = await AndroidSupport.ShowList (Localization.GetText ("SelectNotification"),
					Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel), list);

			// Установка результата
			if ((e == null) || (res >= 0))
				{
				currentNotification = res;
				selectedNotification.Text = GetShortNotificationName (list[res]);

				nameField.Text = ProgramDescription.NSet.Notifications[res].Name;
				linkField = ProgramDescription.NSet.Notifications[res].Link;
				beginningField = ProgramDescription.NSet.Notifications[res].Beginning;
				endingField = ProgramDescription.NSet.Notifications[res].Ending;

				currentOcc = ProgramDescription.NSet.Notifications[res].OccurrenceNumber;
				OccurrenceChanged (null, null);
				currentFreq = ProgramDescription.NSet.Notifications[res].UpdateFrequency;
				RequestStepChanged (null, null);

				enabledSwitch.IsToggled = ProgramDescription.NSet.Notifications[res].IsEnabled;
				notifyIfUnavailableSwitch.IsToggled =
					ProgramDescription.NSet.Notifications[res].NotifyIfSourceIsUnavailable;

				comparatorSwitch.IsToggled = (ProgramDescription.NSet.Notifications[res].ComparisonType !=
					NotComparatorTypes.Disabled);
				if (comparatorSwitch.IsToggled)
					{
					comparatorType = ProgramDescription.NSet.Notifications[res].ComparisonType;
					ComparatorTypeChanged (null, null);
					}

				comparatorValueField.Text = ProgramDescription.NSet.Notifications[res].ComparisonString;
				ignoreMisfitsSwitch.IsToggled = ProgramDescription.NSet.Notifications[res].IgnoreComparisonMisfits;
				}

			// Сброс
			list.Clear ();
			}

		// Метод возвращает усечённое имя оповещения
		private string GetShortNotificationName (string Name)
			{
			string res = Name;

			if (res.Length > 20)
				res = res.Substring (0, 17) + "...";

			return res;
			}

		// Изменение порядкового номера вхождения
		private async void OccurrenceChanged (object sender, EventArgs e)
			{
			// Изменение значения
			if (sender != null)
				{
				// Подсказки
				if (!NotificationsSupport.GetTipState (NSTipTypes.OccurenceTip))
					await ShowTips (NSTipTypes.OccurenceTip);

				Xamarin.Forms.Button b = (Xamarin.Forms.Button)sender;
				if (AndroidSupport.IsNameDefault (b.Text, ASButtonDefaultTypes.Increase) &&
					(currentOcc < Notification.MaxOccurrenceNumber))
					currentOcc++;
				else if (AndroidSupport.IsNameDefault (b.Text, ASButtonDefaultTypes.Decrease) &&
					(currentOcc > 1))
					currentOcc--;
				}

			occFieldLabel.Text = string.Format (Localization.GetText ("OccFieldLabel"), currentOcc);
			}

		// Изменение значения частоты опроса
		private void RequestStepChanged (object sender, EventArgs e)
			{
			if (e != null)
				{
				Xamarin.Forms.Button b = (Xamarin.Forms.Button)sender;

				if (AndroidSupport.IsNameDefault (b.Text, ASButtonDefaultTypes.Increase) &&
					(currentFreq < NotificationsSupport.MaxBackgroundRequestStep))
					{
					currentFreq++;
					requestStepIncreased = true;
					}

				else if (AndroidSupport.IsNameDefault (b.Text, ASButtonDefaultTypes.Decrease) &&
					(currentFreq > 1))
					{
					currentFreq--;
					requestStepIncreased = false;
					}

				else if (AndroidSupport.IsNameDefault (b.Text, ASButtonDefaultTypes.Create))
					{
					if (requestStepIncreased)
						{
						if (currentFreq < NotificationsSupport.MaxBackgroundRequestStep - 4)
							currentFreq += 5;
						else
							currentFreq = NotificationsSupport.MaxBackgroundRequestStep;
						}
					else
						{
						if (currentFreq > 5)
							currentFreq -= 5;
						else
							currentFreq = 1;
						}
					}
				}

			// Обновление
			requestStepFieldLabel.Text = string.Format (Localization.GetText ("BackgroundRequestOn"),
				currentFreq * NotificationsSupport.BackgroundRequestStepMinutes);
			}

		// Удаление оповещения
		private async void DeleteNotification (object sender, EventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NSTipTypes.DeleteButton))
				await ShowTips (NSTipTypes.DeleteButton);

			// Контроль
			if (!await AndroidSupport.ShowMessage (Localization.GetText ("DeleteMessage"),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Yes),
				Localization.GetDefaultText (LzDefaultTextValues.Button_No)))
				return;

			// Удаление и переход к другому оповещению
			ProgramDescription.NSet.Notifications.RemoveAt (currentNotification);
			if (currentNotification >= ProgramDescription.NSet.Notifications.Count)
				currentNotification = ProgramDescription.NSet.Notifications.Count - 1;
			SelectNotification (null, null);

			// Обновление контролов
			UpdateNotButtons ();
			}

		// Обновление оповещения
		private async void ApplyNotification (object sender, EventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NSTipTypes.ApplyButton))
				await ShowTips (NSTipTypes.ApplyButton);

			// Обновление (при успехе – обновление названия)
			if (await UpdateItem (currentNotification))
				{
				selectedNotification.Text = GetShortNotificationName (nameField.Text);
				AndroidSupport.ShowBalloon (Localization.GetText ("ApplyMessage") + nameField.Text, false);
				}
			}

		// Общий метод обновления оповещений
		private async Task<bool> UpdateItem (int ItemNumber)
			{
			// Инициализация оповещения
			NotConfiguration cfg;
			cfg.NotificationName = nameField.Text;
			cfg.SourceLink = linkField;
			cfg.WatchAreaBeginningSign = beginningField;
			cfg.WatchAreaEndingSign = endingField;
			cfg.UpdatingFrequency = currentFreq;
			cfg.OccurrenceNumber = currentOcc;
			cfg.ComparisonType = comparatorSwitch.IsToggled ? comparatorType : NotComparatorTypes.Disabled;
			cfg.ComparisonString = comparatorValueField.Text;
			cfg.IgnoreComparisonMisfits = ignoreMisfitsSwitch.IsToggled;
			cfg.NotifyWhenUnavailable = notifyIfUnavailableSwitch.IsToggled;

			Notification ni = new Notification (cfg);

			if (!ni.IsInited)
				{
				await AndroidSupport.ShowMessage (Localization.GetText ("NotEnoughDataMessage"),
					Localization.GetDefaultText (LzDefaultTextValues.Button_OK));

				nameField.Focus ();
				return false;
				}

			// Условие не выполняется только в двух случаях:
			// - когда добавляется новое оповещение, не имеющее аналогов в списке;
			// - когда обновляется текущее выбранное оповещение.
			// Остальные случаи следует считать попыткой задвоения имени
			int idx = ProgramDescription.NSet.Notifications.IndexOf (ni);
			if ((idx >= 0) && (idx != ItemNumber))
				{
				await AndroidSupport.ShowMessage (Localization.GetText ("NotMatchingNames"),
					Localization.GetDefaultText (LzDefaultTextValues.Button_OK));

				nameField.Focus ();
				return false;
				}

			ni.IsEnabled = enabledSwitch.IsToggled;

			// Добавление
			if (ItemNumber < 0)
				ProgramDescription.NSet.Notifications.Add (ni);
			else if (ItemNumber < ProgramDescription.NSet.Notifications.Count)
				ProgramDescription.NSet.Notifications[ItemNumber] = ni;

			// Обновление контролов
			UpdateNotButtons ();
			return true;
			}

		// Обновление кнопок
		private void UpdateNotButtons ()
			{
			notWizardButton.IsEnabled = (ProgramDescription.NSet.Notifications.Count <
				NotificationsSet.MaxNotifications);
			deleteButton.IsVisible = (ProgramDescription.NSet.Notifications.Count > 1);
			}

		// Метод формирует и отправляет шаблон оповещения
		private async void ShareTemplate (object sender, EventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NSTipTypes.ShareNotButton))
				await ShowTips (NSTipTypes.ShareNotButton);

			// Запрос варианта использования
			if (templatesMenuItems.Count < 1)
				{
				templatesMenuItems = new List<string> {
					Localization.GetText ("ShareCurrent"),
					Localization.GetText ("ShareAll")
				};
				}

			int res = await AndroidSupport.ShowList (Localization.GetText ("ShareVariantSelect"),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel),
				templatesMenuItems);

			// Обработка
			switch (res)
				{
				// Отмена
				default:
					notWizardButton.IsEnabled = true;
					return;

				// Формирование и отправка
				case 0:
					await Share.RequestAsync (nameField.Text +
						NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () + linkField +
						NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () + beginningField +
						NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () + endingField +
						NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () + currentOcc.ToString (),
						ProgramDescription.AssemblyVisibleName);
					break;

				case 1:
					await AndroidSupport.SaveToFile (NotificationsSet.SettingsFileName,
						ProgramDescription.NSet.GetSettingsList (), SupportedEncodings.Unicode16);
					break;
				}
			}

		// Выбор ссылки для оповещения
		private async void SpecifyNotificationLink (object sender, EventArgs e)
			{
			// Ссылка
			string res = await AndroidSupport.ShowInput (Localization.GetText ("LinkFieldMessage"),
				null, Localization.GetDefaultText (LzDefaultTextValues.Button_Apply),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Skip),
				150, Keyboard.Url, linkField, Localization.GetText ("LinkFieldPlaceholder"));

			if (!string.IsNullOrWhiteSpace (res))
				linkField = res;

			// Начало
			res = await AndroidSupport.ShowInput (Localization.GetText ("BeginningFieldMessage"),
				null, Localization.GetDefaultText (LzDefaultTextValues.Button_Apply),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Skip),
				Notification.MaxBeginningEndingLength, Keyboard.Url, beginningField,
				Localization.GetText ("BeginningFieldPlaceholder"));

			if (!string.IsNullOrWhiteSpace (res))
				beginningField = res;

			// Конец
			res = await AndroidSupport.ShowInput (Localization.GetText ("EndingFieldMessage"),
				null, Localization.GetDefaultText (LzDefaultTextValues.Button_Apply),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Skip),
				Notification.MaxBeginningEndingLength, Keyboard.Url, endingField,
				Localization.GetText ("BeginningFieldPlaceholder"));

			if (!string.IsNullOrWhiteSpace (res))
				endingField = res;
			}

		// Включение / выключение службы
		private async void ComparatorSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if ((e != null) && !NotificationsSupport.GetTipState (NSTipTypes.ThresholdTip))
				await ShowTips (NSTipTypes.ThresholdTip);

			comparatorTypeButton.IsVisible = comparatorValueField.IsVisible = ignoreMisfitsLabel.IsVisible =
				ignoreMisfitsSwitch.IsVisible = comparatorIncButton.IsVisible = comparatorDecButton.IsVisible =
				comparatorLongButton.IsVisible = comparatorSwitch.IsToggled;

			comparatorLabel.Text = comparatorSwitch.IsToggled ? Localization.GetText ("ComparatorLabel") :
				Localization.GetText ("ComparatorLabelOff");
			}

		// Выбор типа сравнения
		private async void ComparatorTypeChanged (object sender, EventArgs e)
			{
			// Запрос списка оповещений
			int res = (int)comparatorType;

			if (e != null)
				res = await AndroidSupport.ShowList (Localization.GetText ("SelectComparatorType"),
					Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel), comparatorTypes);

			// Установка результата
			if ((e == null) || (res >= 0))
				{
				comparatorType = (NotComparatorTypes)res;
				comparatorTypeButton.Text = comparatorTypes[res];
				}
			}

		// Изменение значения частоты опроса
		private void ComparatorValueChanged (object sender, EventArgs e)
			{
			double comparatorValue = 0.0;
			try
				{
				comparatorValue = double.Parse (comparatorValueField.Text);
				}
			catch { }

			if (e != null)
				{
				Xamarin.Forms.Button b = (Xamarin.Forms.Button)sender;

				if (AndroidSupport.IsNameDefault (b.Text, ASButtonDefaultTypes.Increase))
					{
					comparatorValue += 1.0;
					comparatorValueIncreased = true;
					}

				else if (AndroidSupport.IsNameDefault (b.Text, ASButtonDefaultTypes.Decrease))
					{
					comparatorValue -= 1.0;
					comparatorValueIncreased = false;
					}

				else if (AndroidSupport.IsNameDefault (b.Text, ASButtonDefaultTypes.Create))
					{
					if (comparatorValueIncreased)
						comparatorValue += 5.0;
					else
						comparatorValue -= 5.0;
					}
				}

			// Обновление
			comparatorValueField.Text = comparatorValue.ToString ();
			}

		#endregion
		}
	}
