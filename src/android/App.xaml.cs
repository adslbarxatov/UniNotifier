using Microsoft.Maui.Controls;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает функционал приложения
	/// </summary>
	public partial class App: Application
		{
		#region Общие переменные и константы

		// Флаги прав доступа
		private RDAppStartupFlags flags;

		// Главный журнал приложения
		private List<MainLogItem> masterLog;

		// Управление центральной кнопкой журнала
		private bool centerButtonEnabled = true;
		private const string semaphoreOn = "●";
		private const string semaphoreOff = "○";

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
			logMasterBackColor = Color.FromArgb ("#F0F0F0"),
			logFieldBackColor = Color.FromArgb ("#80808080"),
			logReadModeColor = Color.FromArgb ("#202020"),

			settingsMasterBackColor = Color.FromArgb ("#FFF8F0"),
			settingsFieldBackColor = Color.FromArgb ("#FFE8D0"),

			notSettingsMasterBackColor = Color.FromArgb ("#F0F8FF"),
			notSettingsFieldBackColor = Color.FromArgb ("#D0E8FF"),

			solutionLockedBackColor = Color.FromArgb ("#F0F0F0"),

			aboutMasterBackColor = Color.FromArgb ("#F0FFF0"),
			aboutFieldBackColor = Color.FromArgb ("#D0FFD0");

		#endregion

		#region Переменные страниц

		private ContentPage settingsPage, notSettingsPage, aboutPage, logPage;

		private Label aboutLabel, occFieldLabel, fontSizeFieldLabel, requestStepFieldLabel,
			allowSoundLabel, allowLightLabel, allowVibroLabel, comparatorLabel, ignoreMisfitsLabel,
			aboutFontSizeField;

		private Switch allowStart, enabledSwitch, nightModeSwitch,
			allowSoundSwitch, allowLightSwitch, allowVibroSwitch,
			comparatorSwitch, ignoreMisfitsSwitch, notifyIfUnavailableSwitch, newsAtTheEndSwitch,
			keepScreenOnSwitch;

		private Button selectedNotification, applyButton, deleteButton,
			notWizardButton, comparatorTypeButton, comparatorIncButton,
			comparatorLongButton, comparatorDecButton, centerButtonFunction, linkFieldButton,
			centerButton, languageButton, scrollUpButton, scrollDownButton, menuButton;

		private Editor nameField, comparatorValueField;

		private ListView mainLog;

		private List<string> pageVariants = new List<string> ();

		#endregion

		#region Запуск и настройка

		/// <summary>
		/// Конструктор. Точка входа приложения
		/// </summary>
		public App ()
			{
			// Инициализация
			InitializeComponent ();
			flags = AndroidSupport.GetAppStartupFlags (RDAppStartupFlags.Huawei | RDAppStartupFlags.CanReadFiles |
				RDAppStartupFlags.CanWriteFiles | RDAppStartupFlags.CanShowNotifications);

			if (ProgramDescription.NSet == null)
				ProgramDescription.NSet = new NotificationsSet (false);
			ProgramDescription.NSet.HasUrgentNotifications = false;

			// Переход в статус запуска для отмены вызова из оповещения
			AndroidSupport.AppIsRunning = true;

			// Список типов компараторов
			char[] ctSplitter = new char[] { '\n' };
			comparatorTypes = new List<string> (RDLocale.GetText ("ComparatorTypes").Split (ctSplitter));

			// Общая конструкция страниц приложения
			MainPage = new MasterPage ();

			settingsPage = AndroidSupport.ApplyPageSettings (new SettingsPage (), "SettingsPage",
				RDLocale.GetText ("SettingsPage"), settingsMasterBackColor);
			notSettingsPage = AndroidSupport.ApplyPageSettings (new NotSettingsPage (), "NotSettingsPage",
				RDLocale.GetText ("NotSettingsPage"), notSettingsMasterBackColor);
			aboutPage = AndroidSupport.ApplyPageSettings (new AboutPage (), "AboutPage",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout), aboutMasterBackColor);
			logPage = AndroidSupport.ApplyPageSettings (new LogPage (), "LogPage",
				RDLocale.GetText ("LogPage"), logMasterBackColor);

			AndroidSupport.SetMasterPage (MainPage, logPage, logMasterBackColor);

			if (!NotificationsSet.TipsState.HasFlag (NSTipTypes.PolicyTip))
				AndroidSupport.SetCurrentPage (notSettingsPage, notSettingsMasterBackColor);

			#region Настройки службы

			AndroidSupport.ApplyLabelSettings (settingsPage, "ServiceSettingsLabel",
				RDLocale.GetText ("ServiceSettingsLabel"), RDLabelTypes.HeaderLeft);

			AndroidSupport.ApplyLabelSettings (settingsPage, "AllowStartLabel",
				RDLocale.GetText ("AllowStartLabel"), RDLabelTypes.DefaultLeft);
			allowStart = AndroidSupport.ApplySwitchSettings (settingsPage, "AllowStartSwitch",
				false, settingsFieldBackColor, AllowStart_Toggled, NotificationsSupport.AllowServiceToStart);

			Label allowServiceTip;
			Button allowServiceButton;

			// Не работают оповещения
			if (!flags.HasFlag (RDAppStartupFlags.CanShowNotifications))
				{
				allowStart.IsEnabled = false;
				allowServiceTip = AndroidSupport.ApplyLabelSettings (settingsPage, "AllowStartTip",
					RDLocale.GetDefaultText (RDLDefaultTexts.Message_NotificationPermission), RDLabelTypes.ErrorTip);

				allowServiceButton = AndroidSupport.ApplyButtonSettings (settingsPage, "AllowStartButton",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_GoTo),
					settingsFieldBackColor, CallAppSettings, false);
				allowServiceButton.HorizontalOptions = LayoutOptions.Center;
				}

			// Не работают файловые операции
			else if (!flags.HasFlag (RDAppStartupFlags.CanReadFiles) ||
				!flags.HasFlag (RDAppStartupFlags.CanWriteFiles))
				{
				allowServiceTip = AndroidSupport.ApplyLabelSettings (settingsPage, "AllowStartTip",
					RDLocale.GetDefaultText (RDLDefaultTexts.Message_ReadWritePermission), RDLabelTypes.ErrorTip);

				allowServiceButton = AndroidSupport.ApplyButtonSettings (settingsPage, "AllowStartButton",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Open),
					settingsFieldBackColor, CallAppSettings, false);
				allowServiceButton.HorizontalOptions = LayoutOptions.Center;
				}

			// Общее предупреждение по оповещениям для Android 12 и выше
			else if (!AndroidSupport.IsForegroundStartableFromResumeEvent)
				{
				allowServiceTip = AndroidSupport.ApplyLabelSettings (settingsPage, "AllowStartTip",
					string.Format (RDLocale.GetText ("AllowStartTip"), ProgramDescription.AssemblyMainName),
					RDLabelTypes.Tip);

				allowServiceButton = AndroidSupport.ApplyButtonSettings (settingsPage, "AllowStartButton",
					" ", settingsFieldBackColor, null, false);
				allowServiceButton.IsVisible = false;
				}

			// Нормальный запуск
			else
				{
				allowServiceTip = AndroidSupport.ApplyLabelSettings (settingsPage, "AllowStartTip",
					" ", RDLabelTypes.Tip);
				allowServiceTip.IsVisible = false;

				allowServiceButton = AndroidSupport.ApplyButtonSettings (settingsPage, "AllowStartButton",
					" ", settingsFieldBackColor, null, false);
				allowServiceButton.IsVisible = false;
				}

			AndroidSupport.ApplyLabelSettings (settingsPage, "NotWizardLabel",
				RDLocale.GetText ("NotWizardLabel"), RDLabelTypes.HeaderLeft);
			notWizardButton = AndroidSupport.ApplyButtonSettings (settingsPage, "NotWizardButton",
				RDLocale.GetText ("NotWizardButton"), settingsFieldBackColor, StartNotificationsWizard, false);

			allowSoundLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "AllowSoundLabel",
				RDLocale.GetText ("AllowSoundLabel"), RDLabelTypes.DefaultLeft);
			allowSoundSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "AllowSoundSwitch",
				false, settingsFieldBackColor, AllowSound_Toggled, NotificationsSupport.AllowSound);

			allowLightLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "AllowLightLabel",
				RDLocale.GetText ("AllowLightLabel"), RDLabelTypes.DefaultLeft);
			allowLightSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "AllowLightSwitch",
				false, settingsFieldBackColor, AllowLight_Toggled, NotificationsSupport.AllowLight);

			allowVibroLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "AllowVibroLabel",
				RDLocale.GetText ("AllowVibroLabel"), RDLabelTypes.DefaultLeft);
			allowVibroSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "AllowVibroSwitch",
				false, settingsFieldBackColor, AllowVibro_Toggled, NotificationsSupport.AllowVibro);

			AndroidSupport.ApplyLabelSettings (settingsPage, "AppSettingsLabel",
				RDLocale.GetText ("AppSettingsLabel"), RDLabelTypes.HeaderLeft);

			AndroidSupport.ApplyLabelSettings (settingsPage, "KeepScreenOnLabel",
				RDLocale.GetText ("KeepScreenOnLabel"), RDLabelTypes.DefaultLeft);
			keepScreenOnSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "KeepScreenOnSwitch",
				false, settingsFieldBackColor, KeepScreenOnSwitch_Toggled, NotificationsSupport.KeepScreenOn);

			allowLightLabel.IsVisible = allowLightSwitch.IsVisible = allowSoundLabel.IsVisible =
				allowSoundSwitch.IsVisible = allowVibroLabel.IsVisible = allowVibroSwitch.IsVisible =
				AndroidSupport.AreNotificationsConfigurable;

			AndroidSupport.ApplyLabelSettings (settingsPage, "CenterButtonLabel",
				RDLocale.GetText ("CenterButtonLabel"), RDLabelTypes.HeaderLeft);
			centerButtonFunction = AndroidSupport.ApplyButtonSettings (settingsPage, "CenterButtonFunction",
				NotificationsSupport.SpecialFunctionName, settingsFieldBackColor,
				SetSpecialFunction_Clicked, false);

			#endregion

			#region Настройки оповещений

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "SelectionLabel",
				RDLocale.GetText ("SelectionLabel"), RDLabelTypes.HeaderLeft);
			selectedNotification = AndroidSupport.ApplyButtonSettings (notSettingsPage, "SelectedNotification",
				"", notSettingsFieldBackColor, SelectNotification, false);

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "NameFieldLabel",
				RDLocale.GetText ("NameFieldLabel"), RDLabelTypes.DefaultLeft);
			nameField = AndroidSupport.ApplyEditorSettings (notSettingsPage, "NameField", notSettingsFieldBackColor,
				Keyboard.Text, Notification.MaxBeginningEndingLength, "", null, true);
			nameField.Placeholder = RDLocale.GetText ("NameFieldPlaceholder");

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "SettingsLabel",
				RDLocale.GetText ("SettingsLabel"), RDLabelTypes.HeaderLeft);

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "LinkFieldLabel",
				RDLocale.GetText ("LinkFieldLabel"), RDLabelTypes.DefaultLeft);
			linkFieldButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "LinkFieldButton",
				RDDefaultButtons.Select, notSettingsFieldBackColor, SpecifyNotificationLink);

			occFieldLabel = AndroidSupport.ApplyLabelSettings (notSettingsPage, "OccFieldLabel", "",
				RDLabelTypes.DefaultLeft);
			occFieldLabel.TextType = TextType.Html;

			AndroidSupport.ApplyButtonSettings (notSettingsPage, "OccIncButton",
				RDDefaultButtons.Increase, notSettingsFieldBackColor, OccurrenceChanged);
			AndroidSupport.ApplyButtonSettings (notSettingsPage, "OccDecButton",
				RDDefaultButtons.Decrease, notSettingsFieldBackColor, OccurrenceChanged);
			currentOcc = 1;

			requestStepFieldLabel = AndroidSupport.ApplyLabelSettings (notSettingsPage, "RequestStepFieldLabel",
				"", RDLabelTypes.DefaultLeft);
			requestStepFieldLabel.TextType = TextType.Html;

			AndroidSupport.ApplyButtonSettings (notSettingsPage, "RequestStepIncButton",
				RDDefaultButtons.Increase, notSettingsFieldBackColor, RequestStepChanged);
			AndroidSupport.ApplyButtonSettings (notSettingsPage, "RequestStepDecButton",
				RDDefaultButtons.Decrease, notSettingsFieldBackColor, RequestStepChanged);
			AndroidSupport.ApplyButtonSettings (notSettingsPage, "RequestStepLongIncButton",
				RDDefaultButtons.Create, notSettingsFieldBackColor, RequestStepChanged);
			currentFreq = NotificationsSet.DefaultUpdatingFrequency;

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "EnabledLabel",
				RDLocale.GetText ("EnabledLabel"), RDLabelTypes.DefaultLeft);
			enabledSwitch = AndroidSupport.ApplySwitchSettings (notSettingsPage, "EnabledSwitch",
				false, notSettingsFieldBackColor, null, false);

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "AvailabilityLabel",
				RDLocale.GetText ("AvailabilityLabel"), RDLabelTypes.DefaultLeft);
			notifyIfUnavailableSwitch = AndroidSupport.ApplySwitchSettings (notSettingsPage, "AvailabilitySwitch",
				false, notSettingsFieldBackColor, null, false);

			// Новые
			comparatorLabel = AndroidSupport.ApplyLabelSettings (notSettingsPage, "ComparatorLabel",
				RDLocale.GetText ("ComparatorLabelOff"), RDLabelTypes.DefaultLeft);
			comparatorSwitch = AndroidSupport.ApplySwitchSettings (notSettingsPage, "ComparatorSwitch",
				false, notSettingsFieldBackColor, ComparatorSwitch_Toggled, false);
			comparatorTypeButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ComparatorType",
				" ", notSettingsFieldBackColor, ComparatorTypeChanged, true);

			comparatorValueField = AndroidSupport.ApplyEditorSettings (notSettingsPage, "ComparatorValue",
				notSettingsFieldBackColor, Keyboard.Default, 10, "0", null, true);
			comparatorIncButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ComparatorValueIncButton",
				RDDefaultButtons.Increase, notSettingsFieldBackColor, ComparatorValueChanged);
			comparatorDecButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ComparatorValueDecButton",
				RDDefaultButtons.Decrease, notSettingsFieldBackColor, ComparatorValueChanged);
			comparatorLongButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ComparatorValueLongButton",
				RDDefaultButtons.Create, notSettingsFieldBackColor, ComparatorValueChanged);

			ignoreMisfitsLabel = AndroidSupport.ApplyLabelSettings (notSettingsPage, "IgnoreMisfitsLabel",
				RDLocale.GetText ("IgnoreMisfitsLabel"), RDLabelTypes.DefaultLeft);
			ignoreMisfitsSwitch = AndroidSupport.ApplySwitchSettings (notSettingsPage, "IgnoreMisfitsSwitch",
				false, notSettingsFieldBackColor, null, false);

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "UrgentTip",
				RDLocale.GetText ("UrgentTip"), RDLabelTypes.Tip);

			// Инициализация полей
			ComparatorTypeChanged (null, null);
			ComparatorSwitch_Toggled (null, null);
			SelectNotification (null, null);

			#endregion

			#region Управление оповещениями

			applyButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ApplyButton",
				RDDefaultButtons.Apply, notSettingsFieldBackColor, ApplyNotification);
			applyButton.HorizontalOptions = LayoutOptions.Fill;

			deleteButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "DeleteButton",
				RDDefaultButtons.Delete, notSettingsFieldBackColor, DeleteNotification);

			AndroidSupport.ApplyButtonSettings (notSettingsPage, "ShareTemplateButton",
				RDDefaultButtons.Share, notSettingsFieldBackColor, ShareTemplate);

			#endregion

			#region Страница "О программе"

			aboutLabel = AndroidSupport.ApplyLabelSettings (aboutPage, "AboutLabel",
				RDGenerics.AppAboutLabelText, RDLabelTypes.AppAbout);

			AndroidSupport.ApplyButtonSettings (aboutPage, "ManualsButton",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_ReferenceMaterials),
				aboutFieldBackColor, ReferenceButton_Click, false);
			AndroidSupport.ApplyButtonSettings (aboutPage, "HelpButton",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_HelpSupport),
				aboutFieldBackColor, HelpButton_Click, false);
			AndroidSupport.ApplyButtonSettings (aboutPage, "RepeatTips", RDLocale.GetText ("RepeatTips"),
				aboutFieldBackColor, RepeatTips_Clicked, false);
			AndroidSupport.ApplyLabelSettings (aboutPage, "GenericSettingsLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_GenericSettings),
				RDLabelTypes.HeaderLeft);

			UpdateNotButtons ();

			AndroidSupport.ApplyLabelSettings (aboutPage, "RestartTipLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Message_RestartRequired),
				RDLabelTypes.Tip);

			AndroidSupport.ApplyLabelSettings (aboutPage, "LanguageLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_InterfaceLanguage),
				RDLabelTypes.DefaultLeft);
			languageButton = AndroidSupport.ApplyButtonSettings (aboutPage, "LanguageSelector",
				RDLocale.LanguagesNames[(int)RDLocale.CurrentLanguage],
				aboutFieldBackColor, SelectLanguage_Clicked, false);

			AndroidSupport.ApplyLabelSettings (aboutPage, "FontSizeLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_InterfaceFontSize),
				RDLabelTypes.DefaultLeft);
			AndroidSupport.ApplyButtonSettings (aboutPage, "FontSizeInc",
				RDDefaultButtons.Increase, aboutFieldBackColor, FontSizeButton_Clicked);
			AndroidSupport.ApplyButtonSettings (aboutPage, "FontSizeDec",
				RDDefaultButtons.Decrease, aboutFieldBackColor, FontSizeButton_Clicked);
			aboutFontSizeField = AndroidSupport.ApplyLabelSettings (aboutPage, "FontSizeField",
				" ", RDLabelTypes.DefaultCenter);

			AndroidSupport.ApplyLabelSettings (aboutPage, "HelpHeaderLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout),
				RDLabelTypes.HeaderLeft);
			AndroidSupport.ApplyLabelSettings (aboutPage, "HelpTextLabel",
				RDGenerics.GetEncoding (RDEncodings.UTF8).
				GetString ((byte[])RD_AAOW.Properties.Resources.ResourceManager.
				GetObject (RDLocale.GetHelpFilePath ())), RDLabelTypes.SmallLeft);

			FontSizeButton_Clicked (null, null);

			#endregion

			#region Страница журнала приложения

			mainLog = (ListView)logPage.FindByName ("MainLog");
			mainLog.BackgroundColor = logFieldBackColor;
			mainLog.HasUnevenRows = true;
			mainLog.ItemTapped += MainLog_ItemTapped;
			mainLog.ItemTemplate = new DataTemplate (typeof (NotificationView));
			mainLog.SelectionMode = ListViewSelectionMode.None;
			mainLog.SeparatorVisibility = SeparatorVisibility.None;
			mainLog.ItemAppearing += MainLog_ItemAppearing;
			AndroidSupport.MasterPage.Popped += CurrentPageChanged;

			centerButton = AndroidSupport.ApplyButtonSettings (logPage, "CenterButton", " ",
				logFieldBackColor, CenterButton_Click, false);
			centerButton.FontSize += 6;

			scrollUpButton = AndroidSupport.ApplyButtonSettings (logPage, "ScrollUp",
				RDDefaultButtons.Up, logFieldBackColor, ScrollUpButton_Click);
			scrollDownButton = AndroidSupport.ApplyButtonSettings (logPage, "ScrollDown",
				RDDefaultButtons.Down, logFieldBackColor, ScrollDownButton_Click);

			// Режим чтения
			AndroidSupport.ApplyLabelSettings (settingsPage, "ReadModeLabel",
				RDLocale.GetText ("ReadModeLabel"), RDLabelTypes.DefaultLeft);
			nightModeSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "ReadModeSwitch",
				false, settingsFieldBackColor, NightModeSwitch_Toggled, NotificationsSupport.LogReadingMode);

			// Расположение новых записей в конце журнала
			AndroidSupport.ApplyLabelSettings (settingsPage, "NewsAtTheEndLabel",
				RDLocale.GetText ("NewsAtTheEndLabel"), RDLabelTypes.DefaultLeft);
			newsAtTheEndSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "NewsAtTheEndSwitch",
				false, settingsFieldBackColor, NewsAtTheEndSwitch_Toggled, NotificationsSupport.LogNewsItemsAtTheEnd);

			menuButton = AndroidSupport.ApplyButtonSettings (logPage, "MenuButton",
				RDDefaultButtons.Menu, logFieldBackColor, SelectPage);

			#endregion

			#region Прочие настройки

			AndroidSupport.ApplyLabelSettings (settingsPage, "LogSettingsLabel",
				RDLocale.GetText ("LogSettingsLabel"), RDLabelTypes.HeaderLeft);

			// Режим чтения
			NightModeSwitch_Toggled (null, null);

			// Размер шрифта
			fontSizeFieldLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "FontSizeFieldLabel",
				"", RDLabelTypes.DefaultLeft);
			fontSizeFieldLabel.TextType = TextType.Html;

			AndroidSupport.ApplyButtonSettings (settingsPage, "FontSizeIncButton",
				RDDefaultButtons.Increase, settingsFieldBackColor, FontSizeChanged);
			AndroidSupport.ApplyButtonSettings (settingsPage, "FontSizeDecButton",
				RDDefaultButtons.Decrease, settingsFieldBackColor, FontSizeChanged);

			FontSizeChanged (null, null);

			AndroidSupport.ApplyButtonSettings (settingsPage, "SaveQuietSound",
				RDLocale.GetText ("SaveQuietSound"), settingsFieldBackColor,
				SaveQuietSound_Clicked, false);

			#endregion

			// Запуск цикла обратной связи (без ожидания)
			FinishBackgroundRequest ();

			// Принятие соглашений
			ShowStartupTips ();
			}

		// Исправление для сброса текущей позиции журнала
		private async void CurrentPageChanged (object sender, EventArgs e)
			{
			/*if (((CarouselPage)MainPage).Children.IndexOf (((CarouselPage)MainPage).CurrentPage) != 0)*/
			if (AndroidSupport.MasterPage.CurrentPage != logPage)
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
		private async void ShowStartupTips ()
			{
			// Контроль XPUN
			if (!flags.HasFlag (RDAppStartupFlags.Huawei))
				await AndroidSupport.XPUNLoop ();

			// Требование принятия Политики
			if (!NotificationsSet.TipsState.HasFlag (NSTipTypes.PolicyTip))
				{
				await AndroidSupport.PolicyLoop ();
				NotificationsSet.TipsState |= NSTipTypes.PolicyTip;
				}

			// Подсказки
			if (!NotificationsSet.TipsState.HasFlag (NSTipTypes.StartupTips))
				{
				await AndroidSupport.ShowMessage (RDLocale.GetText ("Tip01"),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Next));

				await AndroidSupport.ShowMessage (RDLocale.GetText ("Tip02"),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Next));

				await AndroidSupport.ShowMessage (RDLocale.GetText ("Tip03_1"),
					AndroidSupport.AreNotificationsConfigurable ?
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK) :
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Next));

				if (!AndroidSupport.AreNotificationsConfigurable)
					await AndroidSupport.ShowMessage (RDLocale.GetText ("Tip03_2"),
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));

				NotificationsSet.TipsState |= NSTipTypes.StartupTips;
				}
			}

		// Метод отображает остальные подсказки
		private async Task<bool> ShowTips (NSTipTypes Type)
			{
			// Подсказки
			await AndroidSupport.ShowMessage (RDLocale.GetText ("Tip04_" + ((uint)Type).ToString ("X4")),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));

			NotificationsSet.TipsState |= Type;
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

		// Вызов настроек приложения (для Android 12 и выше)
		private void CallAppSettings (object sender, EventArgs e)
			{
			AndroidSupport.CallAppSettings ();
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
			if (!await AndroidSupport.ShowMessage (RDLocale.GetText ("AllNewsRequest"),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Yes),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_No)))
				return;

			// Блокировка
			SetLogState (false);
			AndroidSupport.ShowBalloon (RDLocale.GetText ("RequestAllStarted"), true);

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
			AndroidSupport.ShowBalloon (RDLocale.GetText ("RequestCompleted"), true);

			if (!NotificationsSet.TipsState.HasFlag (NSTipTypes.MainLogClickMenuTip))
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
					centerButton.TextColor = Color.FromArgb (dark ? "#FF4040" : "#D00000");
				else if (yellow)
					centerButton.TextColor = Color.FromArgb (dark ? "#FFFF40" : "#D0D000");
				else
					centerButton.TextColor = Color.FromArgb (dark ? "#40FF40" : "#00D000");
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
					"☍\t" + RDLocale.GetText ("ShareOption"),
					"❏\t" + RDLocale.GetText ("CopyOption"),
					RDLocale.GetText ("OtherOption")
					});
				tapMenuItems.Add (new List<string> {
					"▷\t" + RDLocale.GetText ("GoToOption"),
					"☍\t" + RDLocale.GetText ("ShareOption"),
					"❏\t" + RDLocale.GetText ("CopyOption"),
					RDLocale.GetText ("OtherOption")
					});
				tapMenuItems.Add (new List<string> {
					"✕\t" + RDLocale.GetText ("RemoveOption")
					});
				tapMenuItems.Add (new List<string> {
					"▷\t" + RDLocale.GetText ("GoToOption"),
					"☍\t" + RDLocale.GetText ("ShareOption"),
					"❏\t" + RDLocale.GetText ("CopyOption"),
					"↺\t" + RDLocale.GetText ("RequestAgainOption"),
					"✎\t" + RDLocale.GetText ("SetupOption"),
					RDLocale.GetText ("OtherOption")
					});
				tapMenuItems.Add (new List<string> {
					"✕\t" + RDLocale.GetText ("RemoveOption"),
					"✂\t" + RDLocale.GetText ("DisableOption")
					});
				}

			// Запрос варианта использования
			if ((notNumber < 0) || (notNumber >= ProgramDescription.NSet.Notifications.Count))
				{
				menuItem = (string.IsNullOrWhiteSpace (notLink) ? 0 : 1);
				menuItem = await AndroidSupport.ShowList (RDLocale.GetText ("SelectOption"),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel),
					tapMenuItems[menuItem]);

				if (menuItem < 0)
					return;

				variant = menuItem + 10;
				if (string.IsNullOrWhiteSpace (notLink))
					variant++;

				// Контроль второго набора
				if (variant > 12)
					{
					menuItem = await AndroidSupport.ShowList (RDLocale.GetText ("SelectOption"),
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), tapMenuItems[2]);
					if (menuItem < 0)
						return;

					variant += menuItem;
					}
				}
			else
				{
				menuItem = await AndroidSupport.ShowList (RDLocale.GetText ("SelectOption"),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), tapMenuItems[3]);
				if (menuItem < 0)
					return;

				variant = menuItem;

				// Контроль второго набора
				if (variant > 4)
					{
					menuItem = await AndroidSupport.ShowList (RDLocale.GetText ("SelectOption"),
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), tapMenuItems[4]);
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
					if (!NotificationsSet.TipsState.HasFlag (NSTipTypes.GoToButton))
						await ShowTips (NSTipTypes.GoToButton);

					try
						{
						await Launcher.OpenAsync (notLink);
						}
					catch
						{
						AndroidSupport.ShowBalloon
							(RDLocale.GetDefaultText (RDLDefaultTexts.Message_BrowserNotAvailable), true);
						}
					break;

				// Поделиться
				case 1:
				case 11:
					if (!NotificationsSet.TipsState.HasFlag (NSTipTypes.ShareButton))
						await ShowTips (NSTipTypes.ShareButton);

					await Share.RequestAsync ((notItem.Header + RDLocale.RNRN + notItem.Text +
						RDLocale.RNRN + notLink).Replace ("\r", ""), ProgramDescription.AssemblyVisibleName);
					break;

				// Скопировать в буфер обмена
				case 2:
				case 12:
					try
						{
						await Clipboard.SetTextAsync ((notItem.Header + RDLocale.RNRN + notItem.Text +
							RDLocale.RNRN + notLink).Replace ("\r", ""));
						AndroidSupport.ShowBalloon (RDLocale.GetText ("CopyMessage"), false);

						}
					catch { }
					break;

				// Повторный опрос
				case 3:
					// Блокировка
					SetLogState (false);
					AndroidSupport.ShowBalloon (RDLocale.GetText ("RequestStarted"), true);

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
					AndroidSupport.ShowBalloon (RDLocale.GetText ("RequestCompleted"), true);
					break;

				// Настройка оповещения
				case 4:
					currentNotification = notNumber;
					SelectNotification (null, null);
					/*((CarouselPage)MainPage).CurrentPage = ((CarouselPage)MainPage).Children[notSettingsTab];*/
					AndroidSupport.SetCurrentPage (notSettingsPage, notSettingsMasterBackColor);
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
				AndroidSupport.ShowBalloon (RDLocale.GetText ("BackgroundRequestInProgress"), true);
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
			AndroidSupport.ShowBalloon (RDLocale.GetText ("RequestStarted"), false);

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
				AndroidSupport.ShowBalloon (RDLocale.GetText ("GMJRequestFailed"), true);
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
			if (!NotificationsSet.TipsState.HasFlag (NSTipTypes.MainLogClickMenuTip))
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

		// Выбор текущей страницы
		private async void SelectPage (object sender, EventArgs e)
			{
			// Запрос варианта
			if (pageVariants.Count < 1)
				{
				pageVariants = new List<string> ()
					{
					RDLocale.GetText ("SettingsPage"),
					RDLocale.GetText ("NotSettingsPage"),
					RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout),
					};
				}

			int res = await AndroidSupport.ShowList (RDLocale.GetDefaultText (RDLDefaultTexts.Button_GoTo),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), pageVariants);
			if (res < 0)
				return;

			// Вызов
			switch (res)
				{
				case 0:
					AndroidSupport.SetCurrentPage (settingsPage, settingsMasterBackColor);
					break;

				case 1:
					AndroidSupport.SetCurrentPage (notSettingsPage, notSettingsMasterBackColor);
					break;

				case 2:
					AndroidSupport.SetCurrentPage (aboutPage, aboutMasterBackColor);
					break;
				}
			}

		#endregion

		#region Основные настройки

		// Включение / выключение фиксации экрана
		private async void KeepScreenOnSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSet.TipsState.HasFlag (NSTipTypes.KeepScreenOnTip))
				await ShowTips (NSTipTypes.KeepScreenOnTip);

			NotificationsSupport.KeepScreenOn = keepScreenOnSwitch.IsToggled;
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
					RDLocale.GetText ("NotificationsWizard"),
					RDLocale.GetText ("TemplateList"),
					RDLocale.GetText ("CopyNotification"),
					RDLocale.GetText ("TemplateClipboard"),
					RDLocale.GetText ("TemplateFile"),
				};
				}

			int res = await AndroidSupport.ShowList (RDLocale.GetText ("TemplateSelect"),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), wizardMenuItems);

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
						AndroidSupport.ShowBalloon (RDLocale.GetText ("UpdatingTemplates"), true);

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

					res = await AndroidSupport.ShowList (RDLocale.GetText ("SelectTemplate"),
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), templatesNames);

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
						await AndroidSupport.ShowMessage (RDLocale.GetText ("CurlyTemplate"),
							RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));

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
						await AndroidSupport.ShowMessage (RDLocale.GetText ("NoTemplateInClipboard"),
							RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
						notWizardButton.IsEnabled = true;
						return;
						}

					// Разбор
					string[] values = text.Split (NotificationsTemplatesProvider.ClipboardTemplateSplitter,
						StringSplitOptions.RemoveEmptyEntries);
					if (values.Length != 5)
						{
						await AndroidSupport.ShowMessage (RDLocale.GetText ("NoTemplateInClipboard"),
							RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
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

					res = await AndroidSupport.ShowList (RDLocale.GetText ("SelectNotification"),
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), list);

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
					// Защита
					notWizardButton.IsEnabled = true;

					if (!flags.HasFlag (RDAppStartupFlags.CanReadFiles))
						{
						await AndroidSupport.ShowMessage (RDLocale.GetDefaultText
							(RDLDefaultTexts.Message_ReadWritePermission),
							RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
						return;
						}

					if (!await AndroidSupport.ShowMessage (RDLocale.GetText ("LoadingWarning"),
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_Yes),
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_No)))
						return;

					// Загрузка
					string settings = await AndroidSupport.LoadFromFile (RDEncodings.Unicode16);
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

				AndroidSupport.ShowBalloon (RDLocale.GetText ("AddAsNewMessage") + nameField.Text, false);
				}

			// Переход к дополнительным опциям
			notWizardButton.IsEnabled = true;
			/*((CarouselPage)MainPage).CurrentPage = ((CarouselPage)MainPage).Children[notSettingsTab];*/
			AndroidSupport.SetCurrentPage (notSettingsPage, notSettingsMasterBackColor);
			}

		// Вызов помощника по созданию оповещений
		private async Task<bool> NotificationsWizard ()
			{
			// Шаг запроса ссылки
			NotConfiguration cfg;

			cfg.SourceLink = await AndroidSupport.ShowInput (ProgramDescription.AssemblyVisibleName,
				RDLocale.GetText ("WizardStep1"),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Next),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel),
				Notification.MaxLinkLength, Keyboard.Url, "", RDLocale.GetText ("LinkFieldPlaceholder"));

			if (string.IsNullOrWhiteSpace (cfg.SourceLink))
				return false;

			// Шаг запроса ключевого слова
			string keyword = await AndroidSupport.ShowInput (ProgramDescription.AssemblyVisibleName,
				RDLocale.GetText ("WizardStep2"),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Next),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel),
				Notification.MaxBeginningEndingLength, Keyboard.Default);

			if (string.IsNullOrWhiteSpace (keyword))
				return false;

			// Запуск
			AndroidSupport.ShowBalloon (RDLocale.GetText ("WizardSearch1"), true);

			string[] delim = await Notification.FindDelimiters (cfg.SourceLink, keyword);
			if (delim == null)
				{
				await AndroidSupport.ShowMessage (RDLocale.GetText ("WizardFailure"),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
				return false;
				}

			// Попытка запроса
			for (cfg.OccurrenceNumber = 1; cfg.OccurrenceNumber <= 3; cfg.OccurrenceNumber++)
				{
				AndroidSupport.ShowBalloon (RDLocale.GetText ("WizardSearch2"), true);

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
					await AndroidSupport.ShowMessage (RDLocale.GetText ("WizardFailure"),
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
					return false;
					}

				// Получен текст, проверка
				string text = not.CurrentText;
				if (text.Length > 300)
					text = text.Substring (0, 297) + "...";

				bool notLastStep = (cfg.OccurrenceNumber < 3);
				if (await AndroidSupport.ShowMessage (RDLocale.GetText (notLastStep ?
					"WizardStep3" : "WizardStep4") + RDLocale.RNRN + "~".PadRight (10, '~') +
					RDLocale.RNRN + text,
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Next),
					RDLocale.GetDefaultText (notLastStep ?
					RDLDefaultTexts.Button_Retry : RDLDefaultTexts.Button_Cancel)))
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
				RDLocale.GetText ("WizardStep5"),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel),
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
			if (!NotificationsSet.TipsState.HasFlag (NSTipTypes.ServiceLaunchTip))
				await ShowTips (NSTipTypes.ServiceLaunchTip);

			NotificationsSupport.AllowServiceToStart = allowStart.IsToggled;
			}

		// Включение / выключение вариантов индикации
		private async void AllowSound_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSet.TipsState.HasFlag (NSTipTypes.IndicationTip))
				await ShowTips (NSTipTypes.IndicationTip);

			NotificationsSupport.AllowSound = allowSoundSwitch.IsToggled;
			}

		private async void AllowLight_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSet.TipsState.HasFlag (NSTipTypes.IndicationTip))
				await ShowTips (NSTipTypes.IndicationTip);

			NotificationsSupport.AllowLight = allowLightSwitch.IsToggled;
			}

		private async void AllowVibro_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSet.TipsState.HasFlag (NSTipTypes.IndicationTip))
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
					scrollUpButton.BackgroundColor = scrollDownButton.BackgroundColor =
					menuButton.BackgroundColor = logReadModeColor;
				NotificationsSupport.LogFontColor = logMasterBackColor;
				}
			else
				{
				logPage.BackgroundColor = mainLog.BackgroundColor = centerButton.BackgroundColor =
					scrollUpButton.BackgroundColor = scrollDownButton.BackgroundColor =
					menuButton.BackgroundColor = logMasterBackColor;
				NotificationsSupport.LogFontColor = logReadModeColor;
				}
			scrollUpButton.TextColor = scrollDownButton.TextColor = menuButton.TextColor =
				NotificationView.CurrentAntiBackColor;

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
				Button b = (Button)sender;
				if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Increase) &&
					(fontSize < AndroidSupport.MaxFontSize))
					fontSize++;
				else if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Decrease) &&
					(fontSize > AndroidSupport.MinFontSize))
					fontSize--;

				NotificationsSupport.LogFontSize = fontSize;
				}

			// Принудительное обновление
			fontSizeFieldLabel.Text = string.Format (RDLocale.GetText ("FontSizeLabel"), fontSize.ToString ());

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
				specialOptions.Add (RDLocale.GetText ("SpecialFunction_Nothing"));
				specialOptions.Add (RDLocale.GetText ("SpecialFunction_AllNews"));

				string[] sources = GMJ.SourceNames;
				if (RDLocale.IsCurrentLanguageRuRu)
					{
					for (int i = 0; i < sources.Length; i++)
						specialOptions.Add (RDLocale.GetText ("SpecialFunction_Get") + sources[i]);
					}
				}

			// Запрос
			int res = await AndroidSupport.ShowList (RDLocale.GetText ("SelectSpecialFunction"),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), specialOptions);

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
			// Защита
			if (!flags.HasFlag (RDAppStartupFlags.CanWriteFiles))
				{
				await AndroidSupport.ShowMessage (RDLocale.GetDefaultText
					(RDLDefaultTexts.Message_ReadWritePermission),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
				return;
				}

			// Запись
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
			await AndroidSupport.CallHelpMaterials (RDHelpMaterials.ReferenceMaterials);
			}

		private async void HelpButton_Click (object sender, EventArgs e)
			{
			await AndroidSupport.CallHelpMaterials (RDHelpMaterials.HelpAndSupport);
			}

		// Изменение размера шрифта интерфейса
		private void FontSizeButton_Clicked (object sender, EventArgs e)
			{
			if (sender != null)
				{
				Button b = (Button)sender;
				if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Increase))
					AndroidSupport.MasterFontSize += 0.5;
				else if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Decrease))
					AndroidSupport.MasterFontSize -= 0.5;
				}

			aboutFontSizeField.Text = AndroidSupport.MasterFontSize.ToString ("F1");
			aboutFontSizeField.FontSize = AndroidSupport.MasterFontSize;
			}

		// Запуск с начала
		private void RepeatTips_Clicked (object sender, EventArgs e)
			{
			NotificationsSet.TipsState = NSTipTypes.PolicyTip | NSTipTypes.StartupTips;
			AndroidSupport.ShowBalloon (RDLocale.GetText ("RepeatTipsMessage"), true);
			}

		#endregion

		#region Настройка оповещений

		// Выбор оповещения
		private async void SelectNotification (object sender, EventArgs e)
			{
			// Подсказки
			if ((e != null) && !NotificationsSet.TipsState.HasFlag (NSTipTypes.CurrentNotButton))
				await ShowTips (NSTipTypes.CurrentNotButton);

			// Запрос списка оповещений
			List<string> list = new List<string> ();
			foreach (Notification element in ProgramDescription.NSet.Notifications)
				list.Add (element.Name);

			int res = currentNotification;
			if (e != null)
				res = await AndroidSupport.ShowList (RDLocale.GetText ("SelectNotification"),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), list);

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
				if (!NotificationsSet.TipsState.HasFlag (NSTipTypes.OccurenceTip))
					await ShowTips (NSTipTypes.OccurenceTip);

				Button b = (Button)sender;
				if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Increase) &&
					(currentOcc < Notification.MaxOccurrenceNumber))
					currentOcc++;
				else if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Decrease) &&
					(currentOcc > 1))
					currentOcc--;
				}

			occFieldLabel.Text = string.Format (RDLocale.GetText ("OccFieldLabel"), currentOcc);
			}

		// Изменение значения частоты опроса
		private void RequestStepChanged (object sender, EventArgs e)
			{
			if (e != null)
				{
				Button b = (Button)sender;

				if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Increase) &&
					(currentFreq < NotificationsSupport.MaxBackgroundRequestStep))
					{
					currentFreq++;
					requestStepIncreased = true;
					}

				else if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Decrease) &&
					(currentFreq > 1))
					{
					currentFreq--;
					requestStepIncreased = false;
					}

				else if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Create))
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
			requestStepFieldLabel.Text = string.Format (RDLocale.GetText ("BackgroundRequestOn"),
				currentFreq * NotificationsSupport.BackgroundRequestStepMinutes);
			}

		// Удаление оповещения
		private async void DeleteNotification (object sender, EventArgs e)
			{
			// Подсказки
			if (!NotificationsSet.TipsState.HasFlag (NSTipTypes.DeleteButton))
				await ShowTips (NSTipTypes.DeleteButton);

			// Контроль
			if (!await AndroidSupport.ShowMessage (RDLocale.GetText ("DeleteMessage"),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Yes),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_No)))
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
			if (!NotificationsSet.TipsState.HasFlag (NSTipTypes.ApplyButton))
				await ShowTips (NSTipTypes.ApplyButton);

			// Обновление (при успехе – обновление названия)
			if (await UpdateItem (currentNotification))
				{
				selectedNotification.Text = GetShortNotificationName (nameField.Text);
				AndroidSupport.ShowBalloon (RDLocale.GetText ("ApplyMessage") + nameField.Text, false);
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
				await AndroidSupport.ShowMessage (RDLocale.GetText ("NotEnoughDataMessage"),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));

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
				await AndroidSupport.ShowMessage (RDLocale.GetText ("NotMatchingNames"),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));

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
			if (!NotificationsSet.TipsState.HasFlag (NSTipTypes.ShareNotButton))
				await ShowTips (NSTipTypes.ShareNotButton);

			// Запрос варианта использования
			if (templatesMenuItems.Count < 1)
				{
				templatesMenuItems = new List<string> {
					RDLocale.GetText ("ShareCurrent"),
					RDLocale.GetText ("ShareAll")
				};
				}

			int res = await AndroidSupport.ShowList (RDLocale.GetText ("ShareVariantSelect"),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel),
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
					// Защита
					if (!flags.HasFlag (RDAppStartupFlags.CanWriteFiles))
						{
						await AndroidSupport.ShowMessage (RDLocale.GetDefaultText
							(RDLDefaultTexts.Message_ReadWritePermission),
							RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
						return;
						}

					await AndroidSupport.SaveToFile (NotificationsSet.SettingsFileName,
						ProgramDescription.NSet.GetSettingsList (), RDEncodings.Unicode16);
					break;
				}
			}

		// Выбор ссылки для оповещения
		private async void SpecifyNotificationLink (object sender, EventArgs e)
			{
			// Ссылка
			string res = await AndroidSupport.ShowInput (RDLocale.GetText ("LinkFieldMessage"),
				null, RDLocale.GetDefaultText (RDLDefaultTexts.Button_Apply),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Skip),
				150, Keyboard.Url, linkField, RDLocale.GetText ("LinkFieldPlaceholder"));

			if (!string.IsNullOrWhiteSpace (res))
				linkField = res;

			// Начало
			res = await AndroidSupport.ShowInput (RDLocale.GetText ("BeginningFieldMessage"),
				null, RDLocale.GetDefaultText (RDLDefaultTexts.Button_Apply),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Skip),
				Notification.MaxBeginningEndingLength, Keyboard.Url, beginningField,
				RDLocale.GetText ("BeginningFieldPlaceholder"));

			if (!string.IsNullOrWhiteSpace (res))
				beginningField = res;

			// Конец
			res = await AndroidSupport.ShowInput (RDLocale.GetText ("EndingFieldMessage"),
				null, RDLocale.GetDefaultText (RDLDefaultTexts.Button_Apply),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Skip),
				Notification.MaxBeginningEndingLength, Keyboard.Url, endingField,
				RDLocale.GetText ("BeginningFieldPlaceholder"));

			if (!string.IsNullOrWhiteSpace (res))
				endingField = res;
			}

		// Включение / выключение службы
		private async void ComparatorSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if ((e != null) && !NotificationsSet.TipsState.HasFlag (NSTipTypes.ThresholdTip))
				await ShowTips (NSTipTypes.ThresholdTip);

			comparatorTypeButton.IsVisible = comparatorValueField.IsVisible = ignoreMisfitsLabel.IsVisible =
				ignoreMisfitsSwitch.IsVisible = comparatorIncButton.IsVisible = comparatorDecButton.IsVisible =
				comparatorLongButton.IsVisible = comparatorSwitch.IsToggled;

			comparatorLabel.Text = comparatorSwitch.IsToggled ? RDLocale.GetText ("ComparatorLabel") :
				RDLocale.GetText ("ComparatorLabelOff");
			}

		// Выбор типа сравнения
		private async void ComparatorTypeChanged (object sender, EventArgs e)
			{
			// Запрос списка оповещений
			int res = (int)comparatorType;

			if (e != null)
				res = await AndroidSupport.ShowList (RDLocale.GetText ("SelectComparatorType"),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), comparatorTypes);

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
				Button b = (Button)sender;

				if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Increase))
					{
					comparatorValue += 1.0;
					comparatorValueIncreased = true;
					}

				else if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Decrease))
					{
					comparatorValue -= 1.0;
					comparatorValueIncreased = false;
					}

				else if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Create))
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
