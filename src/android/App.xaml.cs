using Android.Widget;
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
		private const string centerButtonNormalName = "ƒ";
		private const string centerButtonRequestName = "…";

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
		private List<string> communities = new List<string> ();
		private List<string> languages = new List<string> ();

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
			allowSoundLabel, allowLightLabel, allowVibroLabel, comparatorLabel, ignoreMisfitsLabel;
		private Xamarin.Forms.Switch allowStart, enabledSwitch, readModeSwitch,
			allowSoundSwitch, allowLightSwitch, allowVibroSwitch, indicateOnlyUrgentSwitch,
			comparatorSwitch, ignoreMisfitsSwitch, notifyIfUnavailableSwitch, newsAtTheEndSwitch,
			keepScreenOnSwitch;
		private Xamarin.Forms.Button selectedNotification, applyButton, deleteButton,
			notWizardButton, comparatorTypeButton, comparatorIncButton,
			comparatorLongButton, comparatorDecButton, centerButtonFunction, linkFieldButton, centerButton;
		private Editor nameField, comparatorValueField;
		private Xamarin.Forms.ListView mainLog;

		#endregion

		#region Запуск и работа приложения

		/// <summary>
		/// Конструктор. Точка входа приложения
		/// </summary>
		public App ()
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

			// Общая конструкция страниц приложения
			MainPage = new MasterPage ();

			settingsPage = AndroidSupport.ApplyPageSettings (MainPage, "SettingsPage",
				Localization.GetText ("SettingsPage"), settingsMasterBackColor);
			notSettingsPage = AndroidSupport.ApplyPageSettings (MainPage, "NotSettingsPage",
				Localization.GetText ("NotSettingsPage"), notSettingsMasterBackColor);
			aboutPage = AndroidSupport.ApplyPageSettings (MainPage, "AboutPage",
				Localization.GetText ("AboutPage"), aboutMasterBackColor);
			logPage = AndroidSupport.ApplyPageSettings (MainPage, "LogPage",
				Localization.GetText ("LogPage"), logMasterBackColor);
			AndroidSupport.SetMainPage (MainPage);

			int tab = 0;
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.PolicyTip))
				tab = notSettingsTab;
			((CarouselPage)MainPage).CurrentPage = ((CarouselPage)MainPage).Children[tab];

			#region Настройки службы

			AndroidSupport.ApplyLabelSettings (settingsPage, "ServiceSettingsLabel",
				Localization.GetText ("ServiceSettingsLabel"), AndroidSupport.LabelTypes.HeaderLeft);

			AndroidSupport.ApplyLabelSettings (settingsPage, "AllowStartLabel",
				Localization.GetText ("AllowStartLabel"), AndroidSupport.LabelTypes.DefaultLeft);
			allowStart = AndroidSupport.ApplySwitchSettings (settingsPage, "AllowStartSwitch",
				false, settingsFieldBackColor, AllowStart_Toggled, AndroidSupport.AllowServiceToStart);

			AndroidSupport.ApplyLabelSettings (settingsPage, "NotWizardLabel",
				Localization.GetText ("NotWizardLabel"), AndroidSupport.LabelTypes.HeaderLeft);
			notWizardButton = AndroidSupport.ApplyButtonSettings (settingsPage, "NotWizardButton",
				Localization.GetText ("NotWizardButton"), settingsFieldBackColor, StartNotificationsWizard, false);

			allowSoundLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "AllowSoundLabel",
				Localization.GetText ("AllowSoundLabel"), AndroidSupport.LabelTypes.DefaultLeft);
			allowSoundSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "AllowSoundSwitch",
				false, settingsFieldBackColor, AllowSound_Toggled, NotificationsSupport.AllowSound);

			allowLightLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "AllowLightLabel",
				Localization.GetText ("AllowLightLabel"), AndroidSupport.LabelTypes.DefaultLeft);
			allowLightSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "AllowLightSwitch",
				false, settingsFieldBackColor, AllowLight_Toggled, NotificationsSupport.AllowLight);

			allowVibroLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "AllowVibroLabel",
				Localization.GetText ("AllowVibroLabel"), AndroidSupport.LabelTypes.DefaultLeft);
			allowVibroSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "AllowVibroSwitch",
				false, settingsFieldBackColor, AllowVibro_Toggled, NotificationsSupport.AllowVibro);

			AndroidSupport.ApplyLabelSettings (settingsPage, "IndicateOnlyUrgentLabel",
				Localization.GetText ("IndicateOnlyUrgentLabel"), AndroidSupport.LabelTypes.DefaultLeft);
			indicateOnlyUrgentSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "IndicateOnlyUrgentSwitch",
				false, settingsFieldBackColor, IndicateOnlyUrgent_Toggled,
				NotificationsSupport.IndicateOnlyUrgentNotifications);

			AndroidSupport.ApplyLabelSettings (settingsPage, "AppSettingsLabel",
				Localization.GetText ("AppSettingsLabel"), AndroidSupport.LabelTypes.HeaderLeft);

			AndroidSupport.ApplyLabelSettings (settingsPage, "KeepScreenOnLabel",
				Localization.GetText ("KeepScreenOnLabel"), AndroidSupport.LabelTypes.DefaultLeft);
			keepScreenOnSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "KeepScreenOnSwitch",
				false, settingsFieldBackColor, KeepScreenOnSwitch_Toggled, AndroidSupport.KeepScreenOn);

			allowLightLabel.IsVisible = allowLightSwitch.IsVisible = allowSoundLabel.IsVisible =
				allowSoundSwitch.IsVisible = allowVibroLabel.IsVisible = allowVibroSwitch.IsVisible =
				AndroidSupport.AreNotificationsConfigurable;

			AndroidSupport.ApplyLabelSettings (settingsPage, "CenterButtonLabel",
				Localization.GetText ("CenterButtonLabel"), AndroidSupport.LabelTypes.HeaderLeft);
			centerButtonFunction = AndroidSupport.ApplyButtonSettings (settingsPage, "CenterButtonFunction",
				NotificationsSupport.SpecialFunctionName, settingsFieldBackColor,
				SetSpecialFunction_Clicked, false);

			#endregion

			#region Настройки оповещений

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "SelectionLabel",
				Localization.GetText ("SelectionLabel"), AndroidSupport.LabelTypes.HeaderLeft);
			selectedNotification = AndroidSupport.ApplyButtonSettings (notSettingsPage, "SelectedNotification",
				"", notSettingsFieldBackColor, SelectNotification, false);

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "NameFieldLabel",
				Localization.GetText ("NameFieldLabel"), AndroidSupport.LabelTypes.DefaultLeft);
			nameField = AndroidSupport.ApplyEditorSettings (notSettingsPage, "NameField", notSettingsFieldBackColor,
				Keyboard.Text, Notification.MaxBeginningEndingLength, "", null, true);
			nameField.Placeholder = Localization.GetText ("NameFieldPlaceholder");

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "SettingsLabel",
				Localization.GetText ("SettingsLabel"), AndroidSupport.LabelTypes.HeaderLeft);

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "LinkFieldLabel",
				Localization.GetText ("LinkFieldLabel"), AndroidSupport.LabelTypes.DefaultLeft);
			linkFieldButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "LinkFieldButton",
				AndroidSupport.ButtonsDefaultNames.Select, notSettingsFieldBackColor, SpecifyNotificationLink);

			occFieldLabel = AndroidSupport.ApplyLabelSettings (notSettingsPage, "OccFieldLabel", "",
				AndroidSupport.LabelTypes.DefaultLeft);
			occFieldLabel.TextType = TextType.Html;

			AndroidSupport.ApplyButtonSettings (notSettingsPage, "OccIncButton",
				AndroidSupport.ButtonsDefaultNames.Increase, notSettingsFieldBackColor, OccurrenceChanged);
			AndroidSupport.ApplyButtonSettings (notSettingsPage, "OccDecButton",
				AndroidSupport.ButtonsDefaultNames.Decrease, notSettingsFieldBackColor, OccurrenceChanged);
			currentOcc = 1;

			requestStepFieldLabel = AndroidSupport.ApplyLabelSettings (notSettingsPage, "RequestStepFieldLabel",
				"", AndroidSupport.LabelTypes.DefaultLeft);
			requestStepFieldLabel.TextType = TextType.Html;

			AndroidSupport.ApplyButtonSettings (notSettingsPage, "RequestStepIncButton",
				AndroidSupport.ButtonsDefaultNames.Increase, notSettingsFieldBackColor, RequestStepChanged);
			AndroidSupport.ApplyButtonSettings (notSettingsPage, "RequestStepDecButton",
				AndroidSupport.ButtonsDefaultNames.Decrease, notSettingsFieldBackColor, RequestStepChanged);
			AndroidSupport.ApplyButtonSettings (notSettingsPage, "RequestStepLongIncButton",
				AndroidSupport.ButtonsDefaultNames.Create, notSettingsFieldBackColor, RequestStepChanged);
			currentFreq = NotificationsSet.DefaultUpdatingFrequency;

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "EnabledLabel",
				Localization.GetText ("EnabledLabel"), AndroidSupport.LabelTypes.DefaultLeft);
			enabledSwitch = AndroidSupport.ApplySwitchSettings (notSettingsPage, "EnabledSwitch",
				false, notSettingsFieldBackColor, null, false);

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "AvailabilityLabel",
				Localization.GetText ("AvailabilityLabel"), AndroidSupport.LabelTypes.DefaultLeft);
			notifyIfUnavailableSwitch = AndroidSupport.ApplySwitchSettings (notSettingsPage, "AvailabilitySwitch",
				false, notSettingsFieldBackColor, null, false);

			// Новые
			comparatorLabel = AndroidSupport.ApplyLabelSettings (notSettingsPage, "ComparatorLabel",
				Localization.GetText ("ComparatorLabelOff"), AndroidSupport.LabelTypes.DefaultLeft);
			comparatorSwitch = AndroidSupport.ApplySwitchSettings (notSettingsPage, "ComparatorSwitch",
				false, notSettingsFieldBackColor, ComparatorSwitch_Toggled, false);
			comparatorTypeButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ComparatorType",
				" ", notSettingsFieldBackColor, ComparatorTypeChanged, true);

			comparatorValueField = AndroidSupport.ApplyEditorSettings (notSettingsPage, "ComparatorValue",
				notSettingsFieldBackColor, Keyboard.Numeric, 10, "0", null, true);
			comparatorIncButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ComparatorValueIncButton",
				AndroidSupport.ButtonsDefaultNames.Increase, notSettingsFieldBackColor, ComparatorValueChanged);
			comparatorDecButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ComparatorValueDecButton",
				AndroidSupport.ButtonsDefaultNames.Decrease, notSettingsFieldBackColor, ComparatorValueChanged);
			comparatorLongButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ComparatorValueLongButton",
				AndroidSupport.ButtonsDefaultNames.Create, notSettingsFieldBackColor, ComparatorValueChanged);

			ignoreMisfitsLabel = AndroidSupport.ApplyLabelSettings (notSettingsPage, "IgnoreMisfitsLabel",
				Localization.GetText ("IgnoreMisfitsLabel"), AndroidSupport.LabelTypes.DefaultLeft);
			ignoreMisfitsSwitch = AndroidSupport.ApplySwitchSettings (notSettingsPage, "IgnoreMisfitsSwitch",
				false, notSettingsFieldBackColor, null, false);

			// Расположение кнопки GMJ справа
			AndroidSupport.ApplyLabelSettings (settingsPage, "NewsAtTheEndLabel",
				Localization.GetText ("NewsAtTheEndLabel"), AndroidSupport.LabelTypes.DefaultLeft);
			newsAtTheEndSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "NewsAtTheEndSwitch",
				false, settingsFieldBackColor, NewsAtTheEndSwitch_Toggled, NotificationsSupport.LogNewsItemsAtTheEnd);

			// Инициализация полей
			ComparatorTypeChanged (null, null);
			ComparatorSwitch_Toggled (null, null);
			SelectNotification (null, null);

			#endregion

			#region Управление оповещениями

			applyButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ApplyButton",
				AndroidSupport.ButtonsDefaultNames.Apply, notSettingsFieldBackColor, ApplyNotification);
			applyButton.HorizontalOptions = LayoutOptions.Fill;

			deleteButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "DeleteButton",
				AndroidSupport.ButtonsDefaultNames.Delete, notSettingsFieldBackColor, DeleteNotification);

			AndroidSupport.ApplyButtonSettings (notSettingsPage, "ShareTemplateButton",
				AndroidSupport.ButtonsDefaultNames.Share, notSettingsFieldBackColor, ShareTemplate);

			#endregion

			#region Страница "О программе"

			aboutLabel = AndroidSupport.ApplyLabelSettings (aboutPage, "AboutLabel",
				ProgramDescription.AssemblyTitle + "\n" +
				ProgramDescription.AssemblyDescription + "\n\n" +
				RDGenerics.AssemblyCopyright + "\nv " +
				ProgramDescription.AssemblyVersion +
				"; " + ProgramDescription.AssemblyLastUpdate,
				AndroidSupport.LabelTypes.AppAbout);

			AndroidSupport.ApplyLabelSettings (aboutPage, "ManualsLabel", Localization.GetText ("ManualsLabel"),
				AndroidSupport.LabelTypes.HeaderLeft);
			AndroidSupport.ApplyLabelSettings (aboutPage, "HelpLabel", Localization.GetText ("HelpLabel"),
				AndroidSupport.LabelTypes.HeaderLeft);
			AndroidSupport.ApplyLabelSettings (aboutPage, "LanguageLabel", Localization.GetText ("LanguageLabel"),
				AndroidSupport.LabelTypes.HeaderLeft);

			AndroidSupport.ApplyButtonSettings (aboutPage, "AppPage", Localization.GetText ("AppPage"),
				aboutFieldBackColor, AppButton_Clicked, false);
			AndroidSupport.ApplyButtonSettings (aboutPage, "ManualPage", Localization.GetText ("ManualPage"),
				aboutFieldBackColor, ManualButton_Clicked, false);
			AndroidSupport.ApplyButtonSettings (aboutPage, "ADPPage", Localization.GetText ("ADPPage"),
				aboutFieldBackColor, ADPButton_Clicked, false);
			AndroidSupport.ApplyButtonSettings (aboutPage, "DevPage", Localization.GetText ("DevPage"),
				aboutFieldBackColor, DevButton_Clicked, false);
			AndroidSupport.ApplyButtonSettings (aboutPage, "CommunityPage", RDGenerics.AssemblyCompany,
				aboutFieldBackColor, CommunityButton_Clicked, false);

			UpdateNotButtons ();

			AndroidSupport.ApplyButtonSettings (aboutPage, "LanguageSelector",
				Localization.LanguagesNames[(int)Localization.CurrentLanguage],
				aboutFieldBackColor, SelectLanguage_Clicked, false);

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

			centerButton = AndroidSupport.ApplyButtonSettings (logPage, "CenterButton", centerButtonNormalName,
				logFieldBackColor, CenterButton_Click, false);
			centerButton.Margin = centerButton.Padding = new Thickness (0);

			#endregion

			#region Прочие настройки

			AndroidSupport.ApplyLabelSettings (settingsPage, "LogSettingsLabel",
				Localization.GetText ("LogSettingsLabel"), AndroidSupport.LabelTypes.HeaderLeft);

			// Режим чтения
			AndroidSupport.ApplyLabelSettings (settingsPage, "ReadModeLabel",
				Localization.GetText ("ReadModeLabel"), AndroidSupport.LabelTypes.DefaultLeft);
			readModeSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "ReadModeSwitch",
				false, settingsFieldBackColor, ReadModeSwitch_Toggled, NotificationsSupport.LogReadingMode);

			ReadModeSwitch_Toggled (null, null);

			// Размер шрифта
			fontSizeFieldLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "FontSizeFieldLabel",
				"", AndroidSupport.LabelTypes.DefaultLeft);
			fontSizeFieldLabel.TextType = TextType.Html;

			AndroidSupport.ApplyButtonSettings (settingsPage, "FontSizeIncButton",
				AndroidSupport.ButtonsDefaultNames.Increase, settingsFieldBackColor, FontSizeChanged);
			AndroidSupport.ApplyButtonSettings (settingsPage, "FontSizeDecButton",
				AndroidSupport.ButtonsDefaultNames.Decrease, settingsFieldBackColor, FontSizeChanged);

			FontSizeChanged (null, null);

			#endregion

			// Запуск цикла обратной связи (без ожидания)
			FinishBackgroundRequest ();

			// Принятие соглашений
			ShowStartupTips ();
			}

		// Цикл обратной связи для загрузки текущего журнала, если фоновая служба не успела завершить работу
		private async Task<bool> FinishBackgroundRequest ()
			{
			// Ожидание завершения операции
			SetLogState (false);

			centerButton.Text = centerButtonRequestName;
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
			while (!Localization.IsXPUNClassAcceptable)
				await AndroidSupport.ShowMessage (Localization.InacceptableXPUNClassMessage, "   ");

			// Требование принятия Политики
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.PolicyTip))
				{
				while (!await AndroidSupport.ShowMessage (AndroidSupport.PolicyAcceptionMessage,
					Localization.GetDefaultButtonName (Localization.DefaultButtons.Accept),
					Localization.GetDefaultButtonName (Localization.DefaultButtons.Read)))
					{
					ADPButton_Clicked (null, null);
					}

				NotificationsSupport.SetTipState (NotificationsSupport.TipTypes.PolicyTip);
				}

			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.StartupTips))
				{
				await AndroidSupport.ShowMessage (Localization.GetText ("Tip01"),
					Localization.GetDefaultButtonName (Localization.DefaultButtons.Next));

				await AndroidSupport.ShowMessage (Localization.GetText ("Tip02"),
					Localization.GetDefaultButtonName (Localization.DefaultButtons.Next));

				await AndroidSupport.ShowMessage (Localization.GetText ("Tip03_1"),
					AndroidSupport.AreNotificationsConfigurable ?
					Localization.GetDefaultButtonName (Localization.DefaultButtons.OK) :
					Localization.GetDefaultButtonName (Localization.DefaultButtons.Next));

				if (!AndroidSupport.AreNotificationsConfigurable)
					await AndroidSupport.ShowMessage (Localization.GetText ("Tip03_2"),
						Localization.GetDefaultButtonName (Localization.DefaultButtons.OK));

				NotificationsSupport.SetTipState (NotificationsSupport.TipTypes.StartupTips);
				NotificationsSupport.SetTipState (NotificationsSupport.TipTypes.FrequencyTip);
				}

			// Нежелательно дублировать
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.FrequencyTip))
				{
				await AndroidSupport.ShowMessage (Localization.GetText ("Tip04_21"),
					Localization.GetDefaultButtonName (Localization.DefaultButtons.OK));

				NotificationsSupport.SetTipState (NotificationsSupport.TipTypes.FrequencyTip);
				}
			}

		// Метод отображает остальные подсказки
		private async Task<bool> ShowTips (NotificationsSupport.TipTypes Type)
			{
			// Подсказки
			await AndroidSupport.ShowMessage (Localization.GetText ("Tip04_" + ((int)Type).ToString ()),
				Localization.GetDefaultButtonName (Localization.DefaultButtons.OK));

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
			AndroidSupport.AppIsRunning = true;
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
				Localization.GetDefaultButtonName (Localization.DefaultButtons.Yes),
				Localization.GetDefaultButtonName (Localization.DefaultButtons.No)))
				return;

			// Блокировка
			SetLogState (false);
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestAllStarted"),
				ToastLength.Long).Show ();

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
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestCompleted"),
				ToastLength.Long).Show ();

			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.MainLogClickMenuTip))
				await ShowTips (NotificationsSupport.TipTypes.MainLogClickMenuTip);
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
			// Контроль
			if (masterLog == null)
				return;

			if ((masterLog.Count < 1) || !needsScroll)
				return;

			// Искусственная задержка
			await Task.Delay (100);

			// Промотка с повторением до достижения нужного участка
			if (newsAtTheEndSwitch.IsToggled)
				{
				if (e.ItemIndex > masterLog.Count - 3)
					needsScroll = false;

				mainLog.ScrollTo (masterLog[masterLog.Count - 1], ScrollToPosition.MakeVisible, false);
				}
			else
				{
				if (e.ItemIndex < 2)
					needsScroll = false;

				mainLog.ScrollTo (masterLog[0], ScrollToPosition.MakeVisible, false);
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
			if (centerButtonFunction.Text.Contains ("("))
				centerButton.Text = " ";
			else
				centerButton.Text = centerButtonNormalName;

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
					Localization.GetDefaultButtonName (Localization.DefaultButtons.Cancel),
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
						Localization.GetDefaultButtonName (Localization.DefaultButtons.Cancel), tapMenuItems[2]);
					if (menuItem < 0)
						return;

					variant += menuItem;
					}
				}
			else
				{
				menuItem = await AndroidSupport.ShowList (Localization.GetText ("SelectOption"),
					Localization.GetDefaultButtonName (Localization.DefaultButtons.Cancel), tapMenuItems[3]);
				if (menuItem < 0)
					return;

				variant = menuItem;

				// Контроль второго набора
				if (variant > 4)
					{
					menuItem = await AndroidSupport.ShowList (Localization.GetText ("SelectOption"),
						Localization.GetDefaultButtonName (Localization.DefaultButtons.Cancel), tapMenuItems[4]);
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
					if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.GoToButton))
						await ShowTips (NotificationsSupport.TipTypes.GoToButton);

					try
						{
						await Launcher.OpenAsync (notLink);
						}
					catch
						{
						Toast.MakeText (Android.App.Application.Context,
							AndroidSupport.GetNoRequiredAppMessage (false), ToastLength.Long).Show ();
						}
					break;

				// Поделиться
				case 1:
				case 11:
					if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.ShareButton))
						await ShowTips (NotificationsSupport.TipTypes.ShareButton);

					await Share.RequestAsync ((notItem.Header + "\n\n" + notItem.Text + "\n\n" +
						notLink).Replace ("\r", ""), ProgramDescription.AssemblyVisibleName);
					break;

				// Скопировать в буфер обмена
				case 2:
				case 12:
					try
						{
						await Clipboard.SetTextAsync ((notItem.Header + "\n\n" + notItem.Text + "\n\n" +
							notLink).Replace ("\r", ""));
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("CopyMessage"),
							ToastLength.Short).Show ();
						}
					catch { }
					break;

				// Повторный опрос
				case 3:
					// Блокировка
					SetLogState (false);
					Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestStarted"),
						ToastLength.Long).Show ();

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
					Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestCompleted"),
						ToastLength.Long).Show ();
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
			centerButton.Text = State ? centerButtonNormalName : centerButtonRequestName;
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

		// Ручная прокрутка журнала
		private void CenterButton_Click (object sender, EventArgs e)
			{
			if (!centerButtonEnabled)
				{
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("BackgroundRequestInProgress"),
					ToastLength.Long).Show ();
				return;
				}

			switch (NotificationsSupport.SpecialFunctionNumber)
				{
				// Без функции
				case 0:
					return;

				// Прокрутка
				case 1:
					if (masterLog.Count < 1)
						return;

					if (newsAtTheEndSwitch.IsToggled)
						mainLog.ScrollTo (masterLog[masterLog.Count - 1], ScrollToPosition.MakeVisible, false);
					else
						mainLog.ScrollTo (masterLog[0], ScrollToPosition.MakeVisible, false);
					break;

				// Опрос всех новостей
				case 2:
					AllNewsItems ();
					break;

				// Вызов функций GMJ
				default:
					GetGMJ ();
					break;
				}
			}

		private async void GetGMJ ()
			{
			// Блокировка на время опроса
			SetLogState (false);
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestStarted"),
				ToastLength.Short).Show ();

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
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("GMJRequestFailed"),
					ToastLength.Long).Show ();
				}
			else if (newText.Contains (GMJ.SourceNoReturnPattern))
				{
				Toast.MakeText (Android.App.Application.Context, newText, ToastLength.Long).Show ();
				}
			else
				{
				AddTextToLog (newText);
				needsScroll = true;
				UpdateLog ();
				}

			// Разблокировка
			SetLogState (true);
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.MainLogClickMenuTip))
				await ShowTips (NotificationsSupport.TipTypes.MainLogClickMenuTip);
			}

		#endregion

		#region Основные настройки

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
				Localization.GetDefaultButtonName (Localization.DefaultButtons.Cancel), wizardMenuItems);

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
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("UpdatingTemplates"),
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

					res = await AndroidSupport.ShowList (Localization.GetText ("SelectTemplate"),
						Localization.GetDefaultButtonName (Localization.DefaultButtons.Cancel), templatesNames);

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
							Localization.GetDefaultButtonName (Localization.DefaultButtons.OK));

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
							Localization.GetDefaultButtonName (Localization.DefaultButtons.OK));
						notWizardButton.IsEnabled = true;
						return;
						}

					// Разбор
					string[] values = text.Split (NotificationsTemplatesProvider.ClipboardTemplateSplitter,
						StringSplitOptions.RemoveEmptyEntries);
					if (values.Length != 5)
						{
						await AndroidSupport.ShowMessage (Localization.GetText ("NoTemplateInClipboard"),
							Localization.GetDefaultButtonName (Localization.DefaultButtons.OK));
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
						Localization.GetDefaultButtonName (Localization.DefaultButtons.Cancel), list);

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
					// Запрос имени файла
					notWizardButton.IsEnabled = true;

					if (!await AndroidSupport.ShowMessage (Localization.GetText ("LoadingWarning"),
						Localization.GetDefaultButtonName (Localization.DefaultButtons.Yes),
						Localization.GetDefaultButtonName (Localization.DefaultButtons.No)))
						return;

					// Контроль разрешений
					await Xamarin.Essentials.Permissions.RequestAsync<Xamarin.Essentials.Permissions.StorageRead> ();
					if (await Xamarin.Essentials.Permissions.CheckStatusAsync<Xamarin.Essentials.Permissions.StorageRead> () !=
						PermissionStatus.Granted)
						{
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("LoadingFailure"),
							ToastLength.Long).Show ();
						return;
						}

					// Запрос
					PickOptions po = new PickOptions ();
					po.FileTypes = new FilePickerFileType (new Dictionary<DevicePlatform, IEnumerable<string>>
						{
						{ DevicePlatform.Android, new[] { "*/*" } },
						});
					po.PickerTitle = ProgramDescription.AssemblyVisibleName;

					FileResult fr = await FilePicker.PickAsync (po);
					if (fr == null)
						return;

					// Загрузка
					if (!ProgramDescription.NSet.ReadSettingsFromFile (fr.FullPath))
						{
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("LoadingFailure"),
							ToastLength.Long).Show ();
						return;
						}

					// Сброс состояния
					Toast.MakeText (Android.App.Application.Context, Localization.GetText ("LoadingSuccess"),
						ToastLength.Long).Show ();

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

				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("AddAsNewMessage") +
					nameField.Text, ToastLength.Short).Show ();
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
				Localization.GetDefaultButtonName (Localization.DefaultButtons.Next),
				Localization.GetDefaultButtonName (Localization.DefaultButtons.Cancel),
				Notification.MaxLinkLength, Keyboard.Url, "", Localization.GetText ("LinkFieldPlaceholder"));

			if (string.IsNullOrWhiteSpace (cfg.SourceLink))
				return false;

			// Шаг запроса ключевого слова
			string keyword = await AndroidSupport.ShowInput (ProgramDescription.AssemblyVisibleName,
				Localization.GetText ("WizardStep2"),
				Localization.GetDefaultButtonName (Localization.DefaultButtons.Next),
				Localization.GetDefaultButtonName (Localization.DefaultButtons.Cancel),
				Notification.MaxBeginningEndingLength, Keyboard.Default);

			if (string.IsNullOrWhiteSpace (keyword))
				return false;

			// Запуск
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("WizardSearch1"),
				ToastLength.Long).Show ();

			string[] delim = await Notification.FindDelimiters (cfg.SourceLink, keyword);
			if (delim == null)
				{
				await AndroidSupport.ShowMessage (Localization.GetText ("WizardFailure"),
					Localization.GetDefaultButtonName (Localization.DefaultButtons.OK));
				return false;
				}

			// Попытка запроса
			for (cfg.OccurrenceNumber = 1; cfg.OccurrenceNumber <= 3; cfg.OccurrenceNumber++)
				{
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("WizardSearch2"),
					ToastLength.Long).Show ();

				cfg.NotificationName = "Test";
				cfg.WatchAreaBeginningSign = delim[0];
				cfg.WatchAreaEndingSign = delim[1];
				cfg.UpdatingFrequency = 1;
				cfg.ComparisonType = NotComparatorTypes.Disabled;
				cfg.ComparisonValue = 0.0;
				cfg.IgnoreComparisonMisfits = cfg.NotifyWhenUnavailable = false;

				Notification not = new Notification (cfg);
				if (!await not.Update ())
					{
					await AndroidSupport.ShowMessage (Localization.GetText ("WizardFailure"),
						Localization.GetDefaultButtonName (Localization.DefaultButtons.OK));
					return false;
					}

				// Получен текст, проверка
				string text = not.CurrentText;
				if (text.Length > 300)
					text = text.Substring (0, 297) + "...";

				bool notLastStep = (cfg.OccurrenceNumber < 3);
				if (await AndroidSupport.ShowMessage (Localization.GetText (notLastStep ?
					"WizardStep3" : "WizardStep4") + "\n\n" + "~".PadRight (10, '~') + "\n\n" + text,
					Localization.GetDefaultButtonName (Localization.DefaultButtons.Next),
					Localization.GetDefaultButtonName (notLastStep ?
					Localization.DefaultButtons.Retry : Localization.DefaultButtons.Cancel)))
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
				Localization.GetDefaultButtonName (Localization.DefaultButtons.OK),
				Localization.GetDefaultButtonName (Localization.DefaultButtons.Cancel),
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
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.ServiceLaunchTip))
				await ShowTips (NotificationsSupport.TipTypes.ServiceLaunchTip);

			AndroidSupport.AllowServiceToStart = allowStart.IsToggled;
			}

		// Включение / выключение фиксации экрана
		private async void KeepScreenOnSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.KeepScreenOnTip))
				await ShowTips (NotificationsSupport.TipTypes.KeepScreenOnTip);

			AndroidSupport.KeepScreenOn = keepScreenOnSwitch.IsToggled;
			}

		// Включение / выключение вариантов индикации
		private async void AllowSound_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.IndicationTip))
				await ShowTips (NotificationsSupport.TipTypes.IndicationTip);

			NotificationsSupport.AllowSound = allowSoundSwitch.IsToggled;
			}

		private async void AllowLight_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.IndicationTip))
				await ShowTips (NotificationsSupport.TipTypes.IndicationTip);

			NotificationsSupport.AllowLight = allowLightSwitch.IsToggled;
			}

		private async void AllowVibro_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.IndicationTip))
				await ShowTips (NotificationsSupport.TipTypes.IndicationTip);

			NotificationsSupport.AllowVibro = allowVibroSwitch.IsToggled;
			}

		private async void IndicateOnlyUrgent_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.OnlyUrgent))
				await ShowTips (NotificationsSupport.TipTypes.OnlyUrgent);

			NotificationsSupport.IndicateOnlyUrgentNotifications = indicateOnlyUrgentSwitch.IsToggled;
			}

		// Включение / выключение режима чтения для лога
		private void ReadModeSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			if (e != null)
				NotificationsSupport.LogReadingMode = readModeSwitch.IsToggled;

			if (readModeSwitch.IsToggled)
				{
				logPage.BackgroundColor = mainLog.BackgroundColor = centerButton.BackgroundColor = logReadModeColor;
				NotificationsSupport.LogFontColor = centerButton.TextColor = logMasterBackColor;
				}
			else
				{
				logPage.BackgroundColor = mainLog.BackgroundColor = centerButton.BackgroundColor = logMasterBackColor;
				NotificationsSupport.LogFontColor = centerButton.TextColor = logReadModeColor;
				}

			// Принудительное обновление (только не при старте)
			if (e != null)
				{
				needsScroll = true;
				UpdateLog ();
				}
			}

		// Включение / выключение добавления новостей с конца журнала
		private void NewsAtTheEndSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			if (e != null)
				NotificationsSupport.LogNewsItemsAtTheEnd = newsAtTheEndSwitch.IsToggled;
			}

		// Изменение размера шрифта лога
		private void FontSizeChanged (object sender, EventArgs e)
			{
			uint fontSize = NotificationsSupport.LogFontSize;

			if (e != null)
				{
				Xamarin.Forms.Button b = (Xamarin.Forms.Button)sender;
				if (AndroidSupport.IsNameDefault (b.Text, AndroidSupport.ButtonsDefaultNames.Increase) &&
					(fontSize < NotificationsSupport.MaxFontSize))
					fontSize++;
				else if (AndroidSupport.IsNameDefault (b.Text, AndroidSupport.ButtonsDefaultNames.Decrease) &&
					(fontSize > NotificationsSupport.MinFontSize))
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
				specialOptions.Add (Localization.GetText ("SpecialFunction_None"));
				specialOptions.Add (Localization.GetText ("SpecialFunction_Scroll"));
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
				Localization.GetDefaultButtonName (Localization.DefaultButtons.Cancel), specialOptions);

			// Сохранение
			if (res < 0)
				return;

			NotificationsSupport.SpecialFunctionNumber = (uint)res;
			NotificationsSupport.SpecialFunctionName = centerButtonFunction.Text = specialOptions[res];
			centerButton.Text = centerButtonNormalName;

			if (res > 2)
				GMJ.SourceNumber = (uint)(res - 3);
			}

		#endregion

		#region О приложении

		// Выбор языка приложения
		private async void SelectLanguage_Clicked (object sender, EventArgs e)
			{
			// Запрос
			if (languages.Count < 1)
				languages = new List<string> (Localization.LanguagesNames);
			int res = await AndroidSupport.ShowList (Localization.GetText ("SelectLanguage"),
				Localization.GetDefaultButtonName (Localization.DefaultButtons.Cancel), languages);

			// Сохранение
			if (res >= 0)
				{
				Localization.CurrentLanguage = (SupportedLanguages)res;
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RestartApp"),
					ToastLength.Long).Show ();
				}
			}

		// Страница проекта
		private async void AppButton_Clicked (object sender, EventArgs e)
			{
			try
				{
				await Launcher.OpenAsync (RDGenerics.DefaultGitLink + ProgramDescription.AssemblyMainName);
				}
			catch
				{
				Toast.MakeText (Android.App.Application.Context,
					AndroidSupport.GetNoRequiredAppMessage (false), ToastLength.Long).Show ();
				}
			}

		// Страница видеоруководства
		private async void ManualButton_Clicked (object sender, EventArgs e)
			{
			try
				{
				await Launcher.OpenAsync (ProgramDescription.AssemblyVideoLink);
				}
			catch
				{
				Toast.MakeText (Android.App.Application.Context,
					AndroidSupport.GetNoRequiredAppMessage (false), ToastLength.Long).Show ();
				}
			}

		// Страница лаборатории
		private async void CommunityButton_Clicked (object sender, EventArgs e)
			{
			if (communities.Count < 1)
				communities = new List<string> (RDGenerics.CommunitiesNames);

			int res = await AndroidSupport.ShowList (Localization.GetText ("CommunitySelect"),
				Localization.GetDefaultButtonName (Localization.DefaultButtons.Cancel), communities);
			if (res < 0)
				return;

			string link = RDGenerics.GetCommunityLink ((uint)res);
			if (string.IsNullOrWhiteSpace (link))
				return;

			try
				{
				await Launcher.OpenAsync (link);
				}
			catch
				{
				Toast.MakeText (Android.App.Application.Context,
					AndroidSupport.GetNoRequiredAppMessage (false), ToastLength.Long).Show ();
				}
			}

		// Страница политики и EULA
		private async void ADPButton_Clicked (object sender, EventArgs e)
			{
			try
				{
				await Launcher.OpenAsync (RDGenerics.ADPLink);
				}
			catch
				{
				Toast.MakeText (Android.App.Application.Context,
					AndroidSupport.GetNoRequiredAppMessage (false), ToastLength.Long).Show ();
				}
			}

		// Страница политики и EULA
		private async void DevButton_Clicked (object sender, EventArgs e)
			{
			try
				{
				EmailMessage message = new EmailMessage
					{
					Subject = RDGenerics.LabMailCaption,
					Body = "",
					To = new List<string> () { RDGenerics.LabMailLink }
					};
				await Email.ComposeAsync (message);
				}
			catch
				{
				Toast.MakeText (Android.App.Application.Context,
					AndroidSupport.GetNoRequiredAppMessage (true), ToastLength.Long).Show ();
				}
			}

		#endregion

		#region Настройка оповещений

		// Выбор оповещения
		private async void SelectNotification (object sender, EventArgs e)
			{
			// Подсказки
			if ((e != null) && !NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.CurrentNotButton))
				await ShowTips (NotificationsSupport.TipTypes.CurrentNotButton);

			// Запрос списка оповещений
			List<string> list = new List<string> ();
			foreach (Notification element in ProgramDescription.NSet.Notifications)
				list.Add (element.Name);

			int res = currentNotification;
			if (e != null)
				res = await AndroidSupport.ShowList (Localization.GetText ("SelectNotification"),
					Localization.GetDefaultButtonName (Localization.DefaultButtons.Cancel), list);

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

				comparatorValueField.Text = ProgramDescription.NSet.Notifications[res].ComparisonValue.ToString ();
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
				if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.OccurenceTip))
					await ShowTips (NotificationsSupport.TipTypes.OccurenceTip);

				Xamarin.Forms.Button b = (Xamarin.Forms.Button)sender;
				if (AndroidSupport.IsNameDefault (b.Text, AndroidSupport.ButtonsDefaultNames.Increase) &&
					(currentOcc < Notification.MaxOccurrenceNumber))
					currentOcc++;
				else if (AndroidSupport.IsNameDefault (b.Text, AndroidSupport.ButtonsDefaultNames.Decrease) &&
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

				if (AndroidSupport.IsNameDefault (b.Text, AndroidSupport.ButtonsDefaultNames.Increase) &&
					(currentFreq < NotificationsSupport.MaxBackgroundRequestStep))
					{
					currentFreq++;
					requestStepIncreased = true;
					}

				else if (AndroidSupport.IsNameDefault (b.Text, AndroidSupport.ButtonsDefaultNames.Decrease) &&
					(currentFreq > 1))
					{
					currentFreq--;
					requestStepIncreased = false;
					}

				else if (AndroidSupport.IsNameDefault (b.Text, AndroidSupport.ButtonsDefaultNames.Create))
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
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.DeleteButton))
				await ShowTips (NotificationsSupport.TipTypes.DeleteButton);

			// Контроль
			if (!await AndroidSupport.ShowMessage (Localization.GetText ("DeleteMessage"),
				Localization.GetDefaultButtonName (Localization.DefaultButtons.Yes),
				Localization.GetDefaultButtonName (Localization.DefaultButtons.No)))
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
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.ApplyButton))
				await ShowTips (NotificationsSupport.TipTypes.ApplyButton);

			// Обновление (при успехе – обновление названия)
			if (await UpdateItem (currentNotification))
				{
				selectedNotification.Text = GetShortNotificationName (nameField.Text);

				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("ApplyMessage") +
					nameField.Text, ToastLength.Short).Show ();
				}
			}

		// Общий метод обновления оповещений
		private async Task<bool> UpdateItem (int ItemNumber)
			{
			// Инициализация оповещения
			double comparatorValue = 0.0;
			try
				{
				comparatorValue = double.Parse (comparatorValueField.Text);
				}
			catch { }

			NotConfiguration cfg;
			cfg.NotificationName = nameField.Text;
			cfg.SourceLink = linkField;
			cfg.WatchAreaBeginningSign = beginningField;
			cfg.WatchAreaEndingSign = endingField;
			cfg.UpdatingFrequency = currentFreq;
			cfg.OccurrenceNumber = currentOcc;
			cfg.ComparisonType = comparatorSwitch.IsToggled ? comparatorType : NotComparatorTypes.Disabled;
			cfg.ComparisonValue = comparatorValue;
			cfg.IgnoreComparisonMisfits = ignoreMisfitsSwitch.IsToggled;
			cfg.NotifyWhenUnavailable = notifyIfUnavailableSwitch.IsToggled;

			Notification ni = new Notification (cfg);

			if (!ni.IsInited)
				{
				await AndroidSupport.ShowMessage (Localization.GetText ("NotEnoughDataMessage"),
					Localization.GetDefaultButtonName (Localization.DefaultButtons.OK));

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
					Localization.GetDefaultButtonName (Localization.DefaultButtons.OK));

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
			notWizardButton.IsEnabled = (ProgramDescription.NSet.Notifications.Count <
				NotificationsSet.MaxNotifications);
			deleteButton.IsVisible = (ProgramDescription.NSet.Notifications.Count > 1);
			}

		// Метод формирует и отправляет шаблон оповещения
		private async void ShareTemplate (object sender, EventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.ShareNotButton))
				await ShowTips (NotificationsSupport.TipTypes.ShareNotButton);

			// Запрос варианта использования
			if (templatesMenuItems.Count < 1)
				{
				templatesMenuItems = new List<string> {
					Localization.GetText ("ShareCurrent"),
					Localization.GetText ("ShareAll")
				};
				}

			int res = await AndroidSupport.ShowList (Localization.GetText ("ShareVariantSelect"),
				Localization.GetDefaultButtonName (Localization.DefaultButtons.Cancel),
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
					// Контроль разрешений
					await Xamarin.Essentials.Permissions.RequestAsync<Xamarin.Essentials.Permissions.StorageWrite> ();
					if (await Xamarin.Essentials.Permissions.CheckStatusAsync<Xamarin.Essentials.Permissions.StorageWrite> () !=
						PermissionStatus.Granted)
						{
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("SharingFailure"),
							ToastLength.Long).Show ();
						return;
						}

					// Получение имени файла
					string fileName;
					try
						{
						if (AndroidSupport.IsStorageDirectlyAccessible)
							{
							fileName = Android.OS.Environment.GetExternalStoragePublicDirectory
								(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
							}
						else
							{
							fileName = Android.App.Application.Context.FilesDir.AbsolutePath;
							}
						}
					catch
						{
						return;
						}

					// Запись
					fileName += ("/" + NotificationsSet.SettingsFileName);
					if (!ProgramDescription.NSet.SaveSettingsToFile (fileName))
						{
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("SharingFailure"),
							ToastLength.Long).Show ();
						return;
						}

					// Дополнительная обработка для новых ОС
					if (!AndroidSupport.IsStorageDirectlyAccessible)
						{
						ShareFile sf = new ShareFile (fileName);

						await Share.RequestAsync (new ShareFileRequest
							{
							File = sf,
							Title = ProgramDescription.AssemblyVisibleName
							});
						}
					else
						{
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("SharingSuccess"),
							ToastLength.Long).Show ();
						}

					break;
				}
			}

		// Выбор ссылки для оповещения
		private async void SpecifyNotificationLink (object sender, EventArgs e)
			{
			// Ссылка
			string res = await AndroidSupport.ShowInput (Localization.GetText ("LinkFieldMessage"),
				null, Localization.GetDefaultButtonName (Localization.DefaultButtons.Apply),
				Localization.GetDefaultButtonName (Localization.DefaultButtons.Skip),
				150, Keyboard.Url, linkField, Localization.GetText ("LinkFieldPlaceholder"));

			if (!string.IsNullOrWhiteSpace (res))
				linkField = res;

			// Начало
			res = await AndroidSupport.ShowInput (Localization.GetText ("BeginningFieldMessage"),
				null, Localization.GetDefaultButtonName (Localization.DefaultButtons.Apply),
				Localization.GetDefaultButtonName (Localization.DefaultButtons.Skip),
				Notification.MaxBeginningEndingLength, Keyboard.Url, beginningField,
				Localization.GetText ("BeginningFieldPlaceholder"));

			if (!string.IsNullOrWhiteSpace (res))
				beginningField = res;

			// Конец
			res = await AndroidSupport.ShowInput (Localization.GetText ("EndingFieldMessage"),
				null, Localization.GetDefaultButtonName (Localization.DefaultButtons.Apply),
				Localization.GetDefaultButtonName (Localization.DefaultButtons.Skip),
				Notification.MaxBeginningEndingLength, Keyboard.Url, endingField,
				Localization.GetText ("BeginningFieldPlaceholder"));

			if (!string.IsNullOrWhiteSpace (res))
				endingField = res;
			}

		// Включение / выключение службы
		private async void ComparatorSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if ((e != null) && !NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.ThresholdTip))
				await ShowTips (NotificationsSupport.TipTypes.ThresholdTip);

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
					Localization.GetDefaultButtonName (Localization.DefaultButtons.Cancel), comparatorTypes);

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

				if (AndroidSupport.IsNameDefault (b.Text, AndroidSupport.ButtonsDefaultNames.Increase))
					{
					comparatorValue += 1.0;
					comparatorValueIncreased = true;
					}

				else if (AndroidSupport.IsNameDefault (b.Text, AndroidSupport.ButtonsDefaultNames.Decrease))
					{
					comparatorValue -= 1.0;
					comparatorValueIncreased = false;
					}

				else if (AndroidSupport.IsNameDefault (b.Text, AndroidSupport.ButtonsDefaultNames.Create))
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
