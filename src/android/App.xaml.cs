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

		private SupportedLanguages al = Localization.CurrentLanguage;
		private List<MainLogItem> masterLog = new List<MainLogItem> (NotificationsSupport.MasterLog);
		private string[] comparatorTypes;
		private const int notSettingsTab = 2;

		private readonly Color
			logMasterBackColor = Color.FromHex ("#F0F0F0"),
			logFieldBackColor = Color.FromHex ("#80808080"),
			logReadModeColor = Color.FromHex ("#202020"),

			solutionMasterBackColor = Color.FromHex ("#FFF8F0"),
			solutionLockedBackColor = Color.FromHex ("#F0F0F0"),
			solutionFieldBackColor = Color.FromHex ("#FFE8D0"),

			aboutMasterBackColor = Color.FromHex ("#F0FFF0"),
			aboutFieldBackColor = Color.FromHex ("#D0FFD0");

		#endregion

		#region Переменные страниц

		private ContentPage settingsPage, notSettingsPage, aboutPage, logPage;
		private Label aboutLabel, occFieldLabel, fontSizeFieldLabel, requestStepFieldLabel,
			allowSoundLabel, allowLightLabel, allowVibroLabel, comparatorLabel, ignoreMisfitsLabel,
			gmjSourceLabel, rightAlignmentLabel;
		private Xamarin.Forms.Switch allowStart, enabledSwitch, readModeSwitch,
			allowSoundSwitch, allowLightSwitch, allowVibroSwitch, indicateOnlyUrgentSwitch,
			comparatorSwitch, ignoreMisfitsSwitch, notifyIfUnavailableSwitch, rightAlignmentSwitch,
			newsAtTheEndSwitch, keepScreenOnSwitch;
		private Xamarin.Forms.Button selectedNotification, applyButton, deleteButton, getGMJButton,
			allNewsButton, notWizardButton, comparatorTypeButton, comparatorIncButton, comparatorLongButton,
			comparatorDecButton, gmjSourceButton, linkFieldButton, statusBar;
		private Editor nameField, beginningField, endingField, comparatorValueField;
		private Grid statusBarGrid;

		private string linkField;
		private Xamarin.Forms.ListView mainLog;
		private uint currentOcc, currentFreq;

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
			comparatorTypes = Localization.GetText ("ComparatorTypes", al).Split (ctSplitter);

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
			AndroidSupport.SetMainPage (MainPage);

			int tab = 0;
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.PolicyTip))
				tab = notSettingsTab;
			((CarouselPage)MainPage).CurrentPage = ((CarouselPage)MainPage).Children[tab];

			#region Настройки службы

			AndroidSupport.ApplyLabelSettings (settingsPage, "ServiceSettingsLabel",
				Localization.GetText ("ServiceSettingsLabel", al), AndroidSupport.LabelTypes.HeaderLeft);

			AndroidSupport.ApplyLabelSettings (settingsPage, "AllowStartLabel",
				Localization.GetText ("AllowStartLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			allowStart = AndroidSupport.ApplySwitchSettings (settingsPage, "AllowStartSwitch",
				false, solutionFieldBackColor, AllowStart_Toggled, AndroidSupport.AllowServiceToStart);

			AndroidSupport.ApplyLabelSettings (settingsPage, "NotWizardLabel",
				Localization.GetText ("NotWizardLabel", al), AndroidSupport.LabelTypes.HeaderLeft);
			notWizardButton = AndroidSupport.ApplyButtonSettings (settingsPage, "NotWizardButton",
				Localization.GetText ("NotWizardButton", al), solutionFieldBackColor, StartNotificationsWizard, false);

			allowSoundLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "AllowSoundLabel",
				Localization.GetText ("AllowSoundLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			allowSoundSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "AllowSoundSwitch",
				false, solutionFieldBackColor, AllowSound_Toggled, NotificationsSupport.AllowSound);

			allowLightLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "AllowLightLabel",
				Localization.GetText ("AllowLightLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			allowLightSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "AllowLightSwitch",
				false, solutionFieldBackColor, AllowLight_Toggled, NotificationsSupport.AllowLight);

			allowVibroLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "AllowVibroLabel",
				Localization.GetText ("AllowVibroLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			allowVibroSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "AllowVibroSwitch",
				false, solutionFieldBackColor, AllowVibro_Toggled, NotificationsSupport.AllowVibro);

			AndroidSupport.ApplyLabelSettings (settingsPage, "IndicateOnlyUrgentLabel",
				Localization.GetText ("IndicateOnlyUrgentLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			indicateOnlyUrgentSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "IndicateOnlyUrgentSwitch",
				false, solutionFieldBackColor, IndicateOnlyUrgent_Toggled, 
				NotificationsSupport.IndicateOnlyUrgentNotifications);

			AndroidSupport.ApplyLabelSettings (settingsPage, "AppSettingsLabel",
				Localization.GetText ("AppSettingsLabel", al), AndroidSupport.LabelTypes.HeaderLeft);

			AndroidSupport.ApplyLabelSettings (settingsPage, "KeepScreenOnLabel",
				Localization.GetText ("KeepScreenOnLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			keepScreenOnSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "KeepScreenOnSwitch",
				false, solutionFieldBackColor, KeepScreenOnSwitch_Toggled, AndroidSupport.KeepScreenOn);

			allowLightLabel.IsVisible = allowLightSwitch.IsVisible = allowSoundLabel.IsVisible =
				allowSoundSwitch.IsVisible = allowVibroLabel.IsVisible = allowVibroSwitch.IsVisible =
				AndroidSupport.AreNotificationsConfigurable;

			gmjSourceLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "GMJSourceLabel",
				Localization.GetText ("GMJSourceLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			gmjSourceButton = AndroidSupport.ApplyButtonSettings (settingsPage, "GMJSource",
				GMJ.SourceName, solutionFieldBackColor, SetGMJSource_Clicked, false);
			gmjSourceButton.IsVisible = gmjSourceLabel.IsVisible = false;//(al == SupportedLanguages.ru_ru);

			#endregion

			#region Настройки оповещений

			selectedNotification = AndroidSupport.ApplyButtonSettings (notSettingsPage, "SelectedNotification",
				"", solutionFieldBackColor, SelectNotification, false);

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "NameFieldLabel",
				Localization.GetText ("NameFieldLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			nameField = AndroidSupport.ApplyEditorSettings (notSettingsPage, "NameField", solutionFieldBackColor,
				Keyboard.Text, Notification.MaxBeginningEndingLength, "", null, true);
			nameField.Placeholder = Localization.GetText ("NameFieldPlaceholder", al);

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "LinkFieldLabel",
				Localization.GetText ("LinkFieldLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			linkFieldButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "LinkFieldButton",
				AndroidSupport.ButtonsDefaultNames.Select, solutionFieldBackColor, SpecifyNotificationLink);

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "BeginningFieldLabel",
				Localization.GetText ("BeginningFieldLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			beginningField = AndroidSupport.ApplyEditorSettings (notSettingsPage, "BeginningField",
				solutionFieldBackColor, Keyboard.Url, Notification.MaxBeginningEndingLength, "", null, true);

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "EndingFieldLabel",
				Localization.GetText ("EndingFieldLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			endingField = AndroidSupport.ApplyEditorSettings (notSettingsPage, "EndingField", solutionFieldBackColor,
				Keyboard.Url, Notification.MaxBeginningEndingLength, "", null, true);

			occFieldLabel = AndroidSupport.ApplyLabelSettings (notSettingsPage, "OccFieldLabel", "",
				AndroidSupport.LabelTypes.DefaultLeft);
			occFieldLabel.TextType = TextType.Html;

			AndroidSupport.ApplyButtonSettings (notSettingsPage, "OccIncButton",
				AndroidSupport.ButtonsDefaultNames.Increase, solutionFieldBackColor, OccurrenceChanged);
			AndroidSupport.ApplyButtonSettings (notSettingsPage, "OccDecButton",
				AndroidSupport.ButtonsDefaultNames.Decrease, solutionFieldBackColor, OccurrenceChanged);
			currentOcc = 1;

			requestStepFieldLabel = AndroidSupport.ApplyLabelSettings (notSettingsPage, "RequestStepFieldLabel",
				"", AndroidSupport.LabelTypes.DefaultLeft);
			requestStepFieldLabel.TextType = TextType.Html;

			AndroidSupport.ApplyButtonSettings (notSettingsPage, "RequestStepIncButton",
				AndroidSupport.ButtonsDefaultNames.Increase, solutionFieldBackColor, RequestStepChanged);
			AndroidSupport.ApplyButtonSettings (notSettingsPage, "RequestStepDecButton",
				AndroidSupport.ButtonsDefaultNames.Decrease, solutionFieldBackColor, RequestStepChanged);
			AndroidSupport.ApplyButtonSettings (notSettingsPage, "RequestStepLongIncButton",
				AndroidSupport.ButtonsDefaultNames.Create, solutionFieldBackColor, RequestStepChanged);
			currentFreq = NotificationsSet.DefaultUpdatingFrequency;

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "EnabledLabel",
				Localization.GetText ("EnabledLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			enabledSwitch = AndroidSupport.ApplySwitchSettings (notSettingsPage, "EnabledSwitch",
				false, solutionFieldBackColor, null, false);

			AndroidSupport.ApplyLabelSettings (notSettingsPage, "AvailabilityLabel",
				Localization.GetText ("AvailabilityLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			notifyIfUnavailableSwitch = AndroidSupport.ApplySwitchSettings (notSettingsPage, "AvailabilitySwitch",
				false, solutionFieldBackColor, null, false);

			// Новые
			comparatorLabel = AndroidSupport.ApplyLabelSettings (notSettingsPage, "ComparatorLabel",
				Localization.GetText ("ComparatorLabelOff", al), AndroidSupport.LabelTypes.DefaultLeft);
			comparatorSwitch = AndroidSupport.ApplySwitchSettings (notSettingsPage, "ComparatorSwitch",
				false, solutionFieldBackColor, ComparatorSwitch_Toggled, false);
			comparatorTypeButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ComparatorType",
				" ", solutionFieldBackColor, ComparatorTypeChanged, true);

			comparatorValueField = AndroidSupport.ApplyEditorSettings (notSettingsPage, "ComparatorValue",
				solutionFieldBackColor, Keyboard.Numeric, 10, "0", null, true);
			comparatorIncButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ComparatorValueIncButton",
				AndroidSupport.ButtonsDefaultNames.Increase, solutionFieldBackColor, ComparatorValueChanged);
			comparatorDecButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ComparatorValueDecButton",
				AndroidSupport.ButtonsDefaultNames.Decrease, solutionFieldBackColor, ComparatorValueChanged);
			comparatorLongButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ComparatorValueLongButton",
				AndroidSupport.ButtonsDefaultNames.Create, solutionFieldBackColor, ComparatorValueChanged);

			ignoreMisfitsLabel = AndroidSupport.ApplyLabelSettings (notSettingsPage, "IgnoreMisfitsLabel",
				Localization.GetText ("IgnoreMisfitsLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			ignoreMisfitsSwitch = AndroidSupport.ApplySwitchSettings (notSettingsPage, "IgnoreMisfitsSwitch",
				false, solutionFieldBackColor, null, false);

			// Расположение кнопки GMJ справа
			statusBarGrid = (Grid)logPage.FindByName ("StatusBarGrid");

			rightAlignmentLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "RightAlignmentLabel",
				Localization.GetText ("RightAlignmentLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			rightAlignmentSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "RightAlignmentSwitch",
				false, solutionFieldBackColor, RightAlignmentSwitch_Toggled, NotificationsSupport.LogButtonsOnTheRightSide);
			rightAlignmentLabel.IsVisible = rightAlignmentSwitch.IsVisible = (al == SupportedLanguages.ru_ru);

			AndroidSupport.ApplyLabelSettings (settingsPage, "NewsAtTheEndLabel",
				Localization.GetText ("NewsAtTheEndLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			newsAtTheEndSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "NewsAtTheEndSwitch",
				false, solutionFieldBackColor, NewsAtTheEndSwitch_Toggled, NotificationsSupport.LogNewsItemsAtTheEnd);

			// Инициализация полей
			ComparatorTypeChanged (null, null);
			ComparatorSwitch_Toggled (null, null);
			SelectNotification (null, null);
			if (al == SupportedLanguages.ru_ru)
				RightAlignmentSwitch_Toggled (null, null);

			#endregion

			#region Управление оповещениями

			applyButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "ApplyButton",
				AndroidSupport.ButtonsDefaultNames.Apply, solutionFieldBackColor, ApplyNotification);
			deleteButton = AndroidSupport.ApplyButtonSettings (notSettingsPage, "DeleteButton",
				AndroidSupport.ButtonsDefaultNames.Delete, solutionFieldBackColor, DeleteNotification);

			AndroidSupport.ApplyButtonSettings (notSettingsPage, "ShareTemplateButton",
				AndroidSupport.ButtonsDefaultNames.Share, solutionFieldBackColor, ShareTemplate);

			#endregion

			#region Страница "О программе"

			aboutLabel = AndroidSupport.ApplyLabelSettings (aboutPage, "AboutLabel",
				ProgramDescription.AssemblyTitle + "\n" +
				ProgramDescription.AssemblyDescription + "\n\n" +
				RDGenerics.AssemblyCopyright + "\nv " +
				ProgramDescription.AssemblyVersion +
				"; " + ProgramDescription.AssemblyLastUpdate,
				AndroidSupport.LabelTypes.AppAbout);

			AndroidSupport.ApplyButtonSettings (aboutPage, "AppPage", Localization.GetText ("AppPage", al),
				aboutFieldBackColor, AppButton_Clicked, false);
			AndroidSupport.ApplyButtonSettings (aboutPage, "ManualPage", Localization.GetText ("ManualPage", al),
				aboutFieldBackColor, ManualButton_Clicked, false);
			AndroidSupport.ApplyButtonSettings (aboutPage, "ADPPage", Localization.GetText ("ADPPage", al),
				aboutFieldBackColor, ADPButton_Clicked, false);
			AndroidSupport.ApplyButtonSettings (aboutPage, "DevPage", Localization.GetText ("DevPage", al),
				aboutFieldBackColor, DevButton_Clicked, false);
			AndroidSupport.ApplyButtonSettings (aboutPage, "CommunityPage", RDGenerics.AssemblyCompany,
				aboutFieldBackColor, CommunityButton_Clicked, false);

			UpdateNotButtons ();

			AndroidSupport.ApplyButtonSettings (aboutPage, "LanguageSelector", Localization.LanguagesNames[(int)al],
				aboutFieldBackColor, SelectLanguage_Clicked, false);
			AndroidSupport.ApplyLabelSettings (aboutPage, "LanguageLabel",
				Localization.GetText ("LanguageLabel", al), AndroidSupport.LabelTypes.DefaultLeft);

			if (al == SupportedLanguages.ru_ru)
				AndroidSupport.ApplyLabelSettings (aboutPage, "Alert", RDGenerics.RuAlertMessage,
					AndroidSupport.LabelTypes.DefaultLeft);

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

			allNewsButton = AndroidSupport.ApplyButtonSettings (logPage, "AllNewsButton",
				AndroidSupport.ButtonsDefaultNames.Refresh, logFieldBackColor, AllNewsItems);
			allNewsButton.Margin = new Thickness (0);
			allNewsButton.FontSize = 22;    // Принудительно, т.к. размер статусбара задан жёстко

			if (al == SupportedLanguages.ru_ru)
				getGMJButton = AndroidSupport.ApplyButtonSettings (logPage, "GetGMJ",
					AndroidSupport.ButtonsDefaultNames.Smile, logFieldBackColor, GetGMJ);
			else
				getGMJButton = AndroidSupport.ApplyButtonSettings (logPage, "GetGMJ",
					AndroidSupport.ButtonsDefaultNames.Refresh, logFieldBackColor, AllNewsItems);
			getGMJButton.Margin = new Thickness (0);
			getGMJButton.FontSize = ((al == SupportedLanguages.ru_ru) ? allNewsButton.FontSize - 2 :
				allNewsButton.FontSize);

			statusBar = AndroidSupport.ApplyButtonSettings (logPage, "StatusBar", " ",
				logFieldBackColor, ScrollLog_Click, false);
			statusBar.FontSize = 15;

			#endregion

			#region Прочие настройки

			AndroidSupport.ApplyLabelSettings (settingsPage, "LogSettingsLabel",
				Localization.GetText ("LogSettingsLabel", al), AndroidSupport.LabelTypes.HeaderLeft);

			// Режим чтения
			AndroidSupport.ApplyLabelSettings (settingsPage, "ReadModeLabel",
				Localization.GetText ("ReadModeLabel", al), AndroidSupport.LabelTypes.DefaultLeft);
			readModeSwitch = AndroidSupport.ApplySwitchSettings (settingsPage, "ReadModeSwitch",
				false, solutionFieldBackColor, ReadModeSwitch_Toggled, NotificationsSupport.LogReadingMode);

			ReadModeSwitch_Toggled (null, null);

			// Размер шрифта
			fontSizeFieldLabel = AndroidSupport.ApplyLabelSettings (settingsPage, "FontSizeFieldLabel",
				"", AndroidSupport.LabelTypes.DefaultLeft);
			fontSizeFieldLabel.TextType = TextType.Html;

			AndroidSupport.ApplyButtonSettings (settingsPage, "FontSizeIncButton",
				AndroidSupport.ButtonsDefaultNames.Increase, solutionFieldBackColor, FontSizeChanged);
			AndroidSupport.ApplyButtonSettings (settingsPage, "FontSizeDecButton",
				AndroidSupport.ButtonsDefaultNames.Decrease, solutionFieldBackColor, FontSizeChanged);

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
			if (!NotificationsSupport.BackgroundRequestInProgress)
				{
				UpdateLog ();
				return false;
				}

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

		// Метод отображает подсказки при первом запуске
		private async void ShowStartupTips ()
			{
			// Контроль XPR
			while (!Localization.IsXPRClassAcceptable)
				await AndroidSupport.ShowMessage (Localization.InacceptableXPRClassMessage, "   ");

			// Требование принятия Политики
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.PolicyTip))
				{
				while (!await AndroidSupport.ShowMessage (Localization.GetText ("PolicyMessage", al),
					Localization.GetText ("AcceptButton", al), Localization.GetText ("DeclineButton", al)))
					{
					ADPButton_Clicked (null, null);
					}

				NotificationsSupport.SetTipState (NotificationsSupport.TipTypes.PolicyTip);
				}

			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.StartupTips))
				{
				await AndroidSupport.ShowMessage (Localization.GetText ("Tip01", al),
					Localization.GetText ("NextButton", al));

				await AndroidSupport.ShowMessage (Localization.GetText ("Tip02", al),
					Localization.GetText ("NextButton", al));

				string tip03 = Localization.GetText ("Tip03_1", al);
				if (!AndroidSupport.AreNotificationsConfigurable)
					tip03 += Localization.GetText ("Tip03_2", al);

				await AndroidSupport.ShowMessage (tip03, Localization.GetText ("NextButton", al));

				NotificationsSupport.SetTipState (NotificationsSupport.TipTypes.StartupTips);
				NotificationsSupport.SetTipState (NotificationsSupport.TipTypes.FrequencyTip);
				}

			// Нежелательно дублировать
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.FrequencyTip))
				{
				await AndroidSupport.ShowMessage (Localization.GetText ("Tip04_21", al),
					Localization.GetText ("NextButton", al));

				NotificationsSupport.SetTipState (NotificationsSupport.TipTypes.FrequencyTip);
				}
			}

		// Метод отображает остальные подсказки
		private async Task<bool> ShowTips (NotificationsSupport.TipTypes Type)
			{
			// Подсказки
			await AndroidSupport.ShowMessage (Localization.GetText ("Tip04_" + ((int)Type).ToString (), al),
				Localization.GetText ("NextButton", al));

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

		#endregion

		#region Журнал

		// Запрос всех новостей
		private int getNotificationIndex = -1;
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

		private async void AllNewsItems (object sender, EventArgs e)
			{
			// Проверка
			if (!await AndroidSupport.ShowMessage (Localization.GetText ("AllNewsRequest", al),
				Localization.GetText ("NextButton", al), Localization.GetText ("CancelButton", al)))
				return;

			// Блокировка
			SetLogState (false);
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestAllStarted", al),
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
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestCompleted", al),
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
		private bool needsScroll = true;

		// Промотка журнала к нужной позиции
		private async void MainLog_ItemAppearing (object sender, ItemVisibilityEventArgs e)
			{
			// Контроль
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
			if (!allNewsButton.IsEnabled || (notItem.StringForSaving == ""))  // Признак разделителя
				return;

			// Сброс состояния
			statusBar.Text = "";

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

			// Запрос варианта использования
			List<string> items;
			int variant = 0;

			if ((notNumber < 0) || (notNumber >= ProgramDescription.NSet.Notifications.Count))
				{
				items = new List<string> {
					"☍\t" + Localization.GetText ("ShareOption", al),
					"❏\t" + Localization.GetText ("CopyOption",al),
					Localization.GetText ("OtherOption", al)
					};
				if (!string.IsNullOrWhiteSpace (notLink))
					items.Insert (0, "▷\t" + Localization.GetText ("GoToOption", al));

				string res = await AndroidSupport.ShowList (Localization.GetText ("SelectOption", al),
					Localization.GetText ("CancelButton", al), items.ToArray ());
				if (!items.Contains (res))
					{
					items.Clear ();
					return;
					}

				variant = items.IndexOf (res) + 10;
				if (string.IsNullOrWhiteSpace (notLink))
					variant++;
				items.Clear ();

				// Контроль второго набора
				if (variant > 12)
					{
					items = new List<string> {
						"✕\t" + Localization.GetText ("RemoveOption", al)
					};

					res = await AndroidSupport.ShowList (Localization.GetText ("SelectOption", al),
						Localization.GetText ("CancelButton", al), items.ToArray ());
					if (!items.Contains (res))
						{
						items.Clear ();
						return;
						}

					variant += items.IndexOf (res);
					items.Clear ();
					}
				}
			else
				{
				items = new List<string> {
					"▷\t" + Localization.GetText ("GoToOption", al),
					"☍\t" + Localization.GetText ("ShareOption", al),
					"❏\t" + Localization.GetText ("CopyOption",al),
					"↺\t" + Localization.GetText ("RequestAgainOption",al),
					"✎\t" + Localization.GetText ("SetupOption", al),
					Localization.GetText ("OtherOption", al)
				};

				string res = await AndroidSupport.ShowList (Localization.GetText ("SelectOption", al),
					Localization.GetText ("CancelButton", al), items.ToArray ());
				if (!items.Contains (res))
					{
					items.Clear ();
					return;
					}

				variant = items.IndexOf (res);
				items.Clear ();

				// Контроль второго набора
				if (variant > 4)
					{
					items = new List<string> {
						"✕\t" + Localization.GetText ("RemoveOption", al),
						"✂\t" + Localization.GetText ("DisableOption", al)
					};

					res = await AndroidSupport.ShowList (Localization.GetText ("SelectOption", al),
						Localization.GetText ("CancelButton", al), items.ToArray ());
					if (!items.Contains (res))
						{
						items.Clear ();
						return;
						}

					variant += items.IndexOf (res);
					items.Clear ();
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
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("WebIsUnavailable", al),
							ToastLength.Long).Show ();
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
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("CopyMessage", al),
							ToastLength.Short).Show ();
						}
					catch { }
					break;

				// Повторный опрос
				case 3:
					// Блокировка
					SetLogState (false);
					Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestStarted", al),
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
					Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RequestCompleted", al),
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
			getGMJButton.IsEnabled = allNewsButton.IsEnabled = State;

			settingsPage.IsEnabled = notSettingsPage.IsEnabled = aboutPage.IsEnabled = State;
			if (!State)
				{
				settingsPage.BackgroundColor = notSettingsPage.BackgroundColor =
					aboutPage.BackgroundColor = solutionLockedBackColor;
				}
			else
				{
				settingsPage.BackgroundColor = notSettingsPage.BackgroundColor = solutionMasterBackColor;
				aboutPage.BackgroundColor = aboutMasterBackColor;
				}

			// Обновление статуса
			if (statusBar.FontAttributes != FontAttributes.None)
				statusBar.FontAttributes = FontAttributes.None;

			if (State)
				statusBar.Text = Localization.GetText ("LogUpdatedMessage", al) + DateTime.Now.ToString ("dd.MM.yy; HH:mm");
			else
				statusBar.Text = Localization.GetText ("RequestStarted", al);
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
			string newText = "";

			for (int i = 0; i < 3; i++)     // Минимизация возможных попаданий в пропуски
				{
				newText = await Task.Run<string> (GMJ.GetRandomGMJ);

				if (!newText.Contains (GMJ.SourceNoReturnPattern))
					break;
				}

			if (newText == "")
				{
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("GMJRequestFailed", al),
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
		private void ScrollLog_Click (object sender, EventArgs e)
			{
			if (masterLog.Count < 1)
				return;

			if (newsAtTheEndSwitch.IsToggled)
				mainLog.ScrollTo (masterLog[masterLog.Count - 1], ScrollToPosition.MakeVisible, false);
			else
				mainLog.ScrollTo (masterLog[0], ScrollToPosition.MakeVisible, false);
			}

		#endregion

		#region Основные настройки

		// Метод запускает мастер оповещений
		private List<string> templatesNames = new List<string> ();
		private async void StartNotificationsWizard (object sender, EventArgs e)
			{
			// Подсказки
			notWizardButton.IsEnabled = false;

			// Запрос варианта использования
			List<string> items = new List<string> {
				Localization.GetText ("NotificationsWizard", al),
				Localization.GetText ("TemplateList", al),
				Localization.GetText ("CopyNotification", al),
				Localization.GetText ("TemplateClipboard", al),
				Localization.GetText ("TemplateFile", al),
				};

			string res = await AndroidSupport.ShowList (Localization.GetText ("TemplateSelect", al),
				Localization.GetText ("CancelButton", al), items.ToArray ());

			// Обработка
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

					res = await AndroidSupport.ShowList (Localization.GetText ("SelectTemplate", al),
						Localization.GetText ("CancelButton", al), templatesNames.ToArray ());

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
						await AndroidSupport.ShowMessage (Localization.GetText ("CurlyTemplate", al),
							Localization.GetText ("NextButton", al));

					// Заполнение
					nameField.Text = ProgramDescription.NSet.NotificationsTemplates.GetName (templateNumber);
					linkField = ProgramDescription.NSet.NotificationsTemplates.GetLink (templateNumber);
					beginningField.Text = ProgramDescription.NSet.NotificationsTemplates.GetBeginning (templateNumber);
					endingField.Text = ProgramDescription.NSet.NotificationsTemplates.GetEnding (templateNumber);
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
						await AndroidSupport.ShowMessage (Localization.GetText ("NoTemplateInClipboard", al),
							Localization.GetText ("NextButton", al));
						notWizardButton.IsEnabled = true;
						return;
						}

					// Разбор
					string[] values = text.Split (NotificationsTemplatesProvider.ClipboardTemplateSplitter,
						StringSplitOptions.RemoveEmptyEntries);
					if (values.Length != 5)
						{
						await AndroidSupport.ShowMessage (Localization.GetText ("NoTemplateInClipboard", al),
							Localization.GetText ("NextButton", al));
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
					currentFreq = NotificationsSet.DefaultUpdatingFrequency;

					break;

				// Создание копированием
				case 2:
					// Запрос списка оповещений
					List<string> list = new List<string> ();
					foreach (Notification element in ProgramDescription.NSet.Notifications)
						list.Add (element.Name);

					res = await AndroidSupport.ShowList (Localization.GetText ("SelectNotification", al),
						Localization.GetText ("CancelButton", al), list.ToArray ());

					// Создание псевдокопии
					int j;
					if ((j = list.IndexOf (res)) >= 0)
						{
						currentNotification = j;
						SelectNotification (null, null);

						nameField.Text = "*" + nameField.Text;
						}

					list.Clear ();
					break;

				// Загрузка из файла
				case 4:
					// Запрос имени файла
					notWizardButton.IsEnabled = true;

					if (!await AndroidSupport.ShowMessage (Localization.GetText ("LoadingWarning", al),
						Localization.GetText ("NextButton", al), Localization.GetText ("CancelButton", al)))
						return;

					// Контроль разрешений
					await Xamarin.Essentials.Permissions.RequestAsync<Xamarin.Essentials.Permissions.StorageRead> ();
					if (await Xamarin.Essentials.Permissions.CheckStatusAsync<Xamarin.Essentials.Permissions.StorageRead> () !=
						PermissionStatus.Granted)
						{
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("LoadingFailure", al),
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
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("LoadingFailure", al),
							ToastLength.Long).Show ();
						return;
						}

					// Сброс состояния
					Toast.MakeText (Android.App.Application.Context, Localization.GetText ("LoadingSuccess", al),
						ToastLength.Long).Show ();

					currentNotification = 0;
					SelectNotification (null, null);
					return;
				}

			// Обновление
			OccurrenceChanged (null, null);
			RequestStepChanged (null, null);
			items.Clear ();

			// Создание уведомления
			enabledSwitch.IsToggled = true;
			notifyIfUnavailableSwitch.IsToggled = false;
			comparatorSwitch.IsToggled = false;

			if (await UpdateItem (-1))
				{
				currentNotification = ProgramDescription.NSet.Notifications.Count - 1;
				SelectNotification (null, null);

				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("AddAsNewMessage", al) + nameField.Text,
					ToastLength.Short).Show ();
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
				Localization.GetText ("WizardStep1", al), Localization.GetText ("NextButton", al),
				Localization.GetText ("CancelButton", al), Notification.MaxLinkLength, Keyboard.Url, "",
				Localization.GetText ("LinkFieldPlaceholder", al));

			if (string.IsNullOrWhiteSpace (cfg.SourceLink))
				return false;

			// Шаг запроса ключевого слова
			string keyword = await AndroidSupport.ShowInput (ProgramDescription.AssemblyVisibleName,
				Localization.GetText ("WizardStep2", al), Localization.GetText ("NextButton", al),
				Localization.GetText ("CancelButton", al), Notification.MaxBeginningEndingLength,
				Keyboard.Default);

			if (string.IsNullOrWhiteSpace (keyword))
				return false;

			// Запуск
			Toast.MakeText (Android.App.Application.Context, Localization.GetText ("WizardSearch1", al),
				ToastLength.Long).Show ();

			string[] delim = await Notification.FindDelimiters (cfg.SourceLink, keyword);
			if (delim == null)
				{
				await AndroidSupport.ShowMessage (Localization.GetText ("WizardFailure", al),
					Localization.GetText ("NextButton", al));
				return false;
				}

			// Попытка запроса
			for (cfg.OccurrenceNumber = 1; cfg.OccurrenceNumber <= 3; cfg.OccurrenceNumber++)
				{
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("WizardSearch2", al),
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
					await AndroidSupport.ShowMessage (Localization.GetText ("WizardFailure", al),
						Localization.GetText ("NextButton", al));
					return false;
					}

				// Получен текст, проверка
				string text = not.CurrentText;
				if (text.Length > 300)
					text = text.Substring (0, 297) + "...";

				if (await AndroidSupport.ShowMessage (Localization.GetText ((cfg.OccurrenceNumber < 3) ?
					"WizardStep3" : "WizardStep4", al) + "\n\n" + "~".PadRight (10, '~') + "\n\n" + text,
					Localization.GetText ("NextButton", al),
					Localization.GetText ((cfg.OccurrenceNumber < 3) ? "RetryButton" : "CancelButton", al)))
					{
					break;
					}
				else
					{
					if (cfg.OccurrenceNumber < 3)
						continue;
					else
						return false;
					}
				}

			// Завершено, запрос названия
			string name = await AndroidSupport.ShowInput (ProgramDescription.AssemblyVisibleName,
				Localization.GetText ("WizardStep5", al), Localization.GetText ("NextButton", al),
				Localization.GetText ("CancelButton", al), Notification.MaxBeginningEndingLength,
				Keyboard.Text);

			if (string.IsNullOrWhiteSpace (name))
				return false;

			// Добавление оповещения
			nameField.Text = name;
			linkField = cfg.SourceLink;
			beginningField.Text = delim[0];
			endingField.Text = delim[1];
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
				logPage.BackgroundColor = mainLog.BackgroundColor = statusBar.BackgroundColor = logReadModeColor;
				allNewsButton.TextColor = getGMJButton.TextColor = statusBar.TextColor =
					NotificationsSupport.LogFontColor = logMasterBackColor;
				}
			else
				{
				logPage.BackgroundColor = mainLog.BackgroundColor = statusBar.BackgroundColor = logMasterBackColor;
				allNewsButton.TextColor = getGMJButton.TextColor = statusBar.TextColor =
					NotificationsSupport.LogFontColor = logReadModeColor;
				}

			// Принудительное обновление (только не при старте)
			if (e != null)
				{
				needsScroll = true;
				UpdateLog ();
				}
			}

		// Включение / выключение правого расположения кнопки GMJ
		private void RightAlignmentSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			if (e != null)
				NotificationsSupport.LogButtonsOnTheRightSide = rightAlignmentSwitch.IsToggled;

			statusBarGrid.FlowDirection = (rightAlignmentSwitch.IsToggled) ? FlowDirection.RightToLeft :
				FlowDirection.LeftToRight;
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
			fontSizeFieldLabel.Text = string.Format (Localization.GetText ("FontSizeLabel", al), fontSize.ToString ());

			if (e != null)
				{
				needsScroll = true;
				UpdateLog ();
				}
			}

		// Выбор языка приложения
		private async void SetGMJSource_Clicked (object sender, EventArgs e)
			{
			// Запрос
			string res = await AndroidSupport.ShowList (Localization.GetText ("SelectGMJSource", al),
				Localization.GetText ("CancelButton", al), GMJ.SourceNames);

			// Сохранение
			List<string> list = new List<string> (GMJ.SourceNames);
			if (list.Contains (res))
				{
				GMJ.SourceNumber = (uint)list.IndexOf (res);
				gmjSourceButton.Text = res;
				}

			list.Clear ();
			}

		#endregion

		#region О приложении

		// Выбор языка приложения
		private async void SelectLanguage_Clicked (object sender, EventArgs e)
			{
			// Запрос
			string res = await AndroidSupport.ShowList (Localization.GetText ("SelectLanguage", al),
				Localization.GetText ("CancelButton", al), Localization.LanguagesNames);

			// Сохранение
			List<string> lngs = new List<string> (Localization.LanguagesNames);
			if (lngs.Contains (res))
				{
				al = (SupportedLanguages)lngs.IndexOf (res);
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("RestartApp", al),
					ToastLength.Long).Show ();
				}

			lngs.Clear ();
			}

		// Страница проекта
		private async void AppButton_Clicked (object sender, EventArgs e)
			{
			try
				{
				await Launcher.OpenAsync (RDGenerics.AssemblyGitLink + ProgramDescription.AssemblyMainName);
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
			string[] comm = RDGenerics.GetCommunitiesNames (al != SupportedLanguages.ru_ru);
			string res = await AndroidSupport.ShowList (Localization.GetText ("CommunitySelect", al),
				Localization.GetText ("CancelButton", al), comm);

			res = RDGenerics.GetCommunityLink (res, al != SupportedLanguages.ru_ru);
			if (string.IsNullOrWhiteSpace (res))
				return;

			try
				{
				await Launcher.OpenAsync (res);
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
				await Launcher.OpenAsync (RDGenerics.GetADPLink (al == SupportedLanguages.ru_ru));
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
					To = new List<string> () { RDGenerics.LabMailLink }
					};
				await Email.ComposeAsync (message);
				}
			catch
				{
				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("EmailsAreUnavailable", al),
					ToastLength.Long).Show ();
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

			string res = list[currentNotification];
			if (e != null)
				res = await AndroidSupport.ShowList (Localization.GetText ("SelectNotification", al),
					Localization.GetText ("CancelButton", al), list.ToArray ());

			// Установка результата
			int i = currentNotification;
			if ((e == null) || ((i = list.IndexOf (res)) >= 0))
				{
				currentNotification = i;
				selectedNotification.Text = GetShortNotificationName (res);

				nameField.Text = ProgramDescription.NSet.Notifications[i].Name;
				linkField = ProgramDescription.NSet.Notifications[i].Link;
				beginningField.Text = ProgramDescription.NSet.Notifications[i].Beginning;
				endingField.Text = ProgramDescription.NSet.Notifications[i].Ending;

				currentOcc = ProgramDescription.NSet.Notifications[i].OccurrenceNumber;
				OccurrenceChanged (null, null);
				currentFreq = ProgramDescription.NSet.Notifications[i].UpdateFrequency;
				RequestStepChanged (null, null);

				enabledSwitch.IsToggled = ProgramDescription.NSet.Notifications[i].IsEnabled;
				notifyIfUnavailableSwitch.IsToggled = ProgramDescription.NSet.Notifications[i].NotifyIfSourceIsUnavailable;

				comparatorSwitch.IsToggled = (ProgramDescription.NSet.Notifications[i].ComparisonType !=
					NotComparatorTypes.Disabled);
				if (comparatorSwitch.IsToggled)
					{
					comparatorType = ProgramDescription.NSet.Notifications[i].ComparisonType;
					ComparatorTypeChanged (null, null);
					}

				comparatorValueField.Text = ProgramDescription.NSet.Notifications[i].ComparisonValue.ToString ();
				ignoreMisfitsSwitch.IsToggled = ProgramDescription.NSet.Notifications[i].IgnoreComparisonMisfits;
				}

			// Сброс
			list.Clear ();
			}
		private int currentNotification = 0;

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

			occFieldLabel.Text = string.Format (Localization.GetText ("OccFieldLabel", al), currentOcc);
			}

		// Изменение значения частоты опроса
		private bool requestStepIncreased = true;
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
			requestStepFieldLabel.Text = string.Format (Localization.GetText ("BackgroundRequestOn", al),
				currentFreq * NotificationsSupport.BackgroundRequestStepMinutes);
			}

		// Удаление оповещения
		private async void DeleteNotification (object sender, EventArgs e)
			{
			// Подсказки
			if (!NotificationsSupport.GetTipState (NotificationsSupport.TipTypes.DeleteButton))
				await ShowTips (NotificationsSupport.TipTypes.DeleteButton);

			// Контроль
			if (!await AndroidSupport.ShowMessage (Localization.GetText ("DeleteMessage", al),
				Localization.GetText ("NextButton", al), Localization.GetText ("CancelButton", al)))
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

				Toast.MakeText (Android.App.Application.Context, Localization.GetText ("ApplyMessage", al) + nameField.Text,
					ToastLength.Short).Show ();
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
			cfg.WatchAreaBeginningSign = beginningField.Text;
			cfg.WatchAreaEndingSign = endingField.Text;
			cfg.UpdatingFrequency = currentFreq;
			cfg.OccurrenceNumber = currentOcc;
			cfg.ComparisonType = comparatorSwitch.IsToggled ? comparatorType : NotComparatorTypes.Disabled;
			cfg.ComparisonValue = comparatorValue;
			cfg.IgnoreComparisonMisfits = ignoreMisfitsSwitch.IsToggled;
			cfg.NotifyWhenUnavailable = notifyIfUnavailableSwitch.IsToggled;

			Notification ni = new Notification (cfg);

			if (!ni.IsInited)
				{
				await AndroidSupport.ShowMessage (Localization.GetText ("NotEnoughDataMessage", al),
					Localization.GetText ("NextButton", al));

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
				await AndroidSupport.ShowMessage (Localization.GetText ("NotMatchingNames", al),
					Localization.GetText ("NextButton", al));

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
			List<string> items = new List<string> {
				Localization.GetText ("ShareCurrent", al),
				Localization.GetText ("ShareAll", al)
				};

			string res = await AndroidSupport.ShowList (Localization.GetText ("ShareVariantSelect", al),
					Localization.GetText ("CancelButton", al), items.ToArray ());

			// Обработка
			switch (items.IndexOf (res))
				{
				// Отмена
				default:
					notWizardButton.IsEnabled = true;
					return;

				// Формирование и отправка
				case 0:
					await Share.RequestAsync (nameField.Text +
						NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () + linkField +
						NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () + beginningField.Text +
						NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () + endingField.Text +
						NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () + currentOcc.ToString (),
						ProgramDescription.AssemblyVisibleName);
					break;

				case 1:
					// Контроль разрешений
					await Xamarin.Essentials.Permissions.RequestAsync<Xamarin.Essentials.Permissions.StorageWrite> ();
					if (await Xamarin.Essentials.Permissions.CheckStatusAsync<Xamarin.Essentials.Permissions.StorageWrite> () !=
						PermissionStatus.Granted)
						{
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("SharingFailure", al),
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
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("SharingFailure", al),
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
						Toast.MakeText (Android.App.Application.Context, Localization.GetText ("SharingSuccess", al),
							ToastLength.Long).Show ();
						}

					break;
				}
			}

		// Выбор ссылки для оповещения
		private async void SpecifyNotificationLink (object sender, EventArgs e)
			{
			// Запрос
			string res = await AndroidSupport.ShowInput (Localization.GetText ("LinkFieldLabel", al),
				null, Localization.GetText ("NextButton", al), Localization.GetText ("CancelButton", al),
				150, Keyboard.Url, linkField, Localization.GetText ("LinkFieldPlaceholder", al));

			if (!string.IsNullOrWhiteSpace (res))
				linkField = res;
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

			comparatorLabel.Text = comparatorSwitch.IsToggled ? Localization.GetText ("ComparatorLabel", al) :
				Localization.GetText ("ComparatorLabelOff", al);
			}

		// Выбор типа сравнения
		private async void ComparatorTypeChanged (object sender, EventArgs e)
			{
			// Запрос списка оповещений
			List<string> list = new List<string> (comparatorTypes);
			int i = (int)comparatorType;
			string res = list[i];

			if (e != null)
				res = await AndroidSupport.ShowList (Localization.GetText ("SelectComparatorType", al),
					Localization.GetText ("CancelButton", al), list.ToArray ());

			// Установка результата
			if ((e == null) || ((i = list.IndexOf (res)) >= 0))
				{
				comparatorType = (NotComparatorTypes)i;
				comparatorTypeButton.Text = res;
				}

			// Сброс
			list.Clear ();
			}
		private NotComparatorTypes comparatorType = NotComparatorTypes.Equal;

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
		private bool comparatorValueIncreased = true;

		#endregion
		}
	}
