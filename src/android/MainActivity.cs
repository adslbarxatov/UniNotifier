﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Views;
using AndroidX.Core.App;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает загрузчик приложения
	/// </summary>
	[Activity (Label = "uNot",
		Icon = "@drawable/launcher_foreground",
		Theme = "@style/SplashTheme",
		MainLauncher = true,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity: MauiAppCompatActivity
		{
		/// <summary>
		/// Принудительная установка масштаба шрифта
		/// </summary>
		/// <param name="base">Существующий набор параметров</param>
		protected override void AttachBaseContext (Context @base)
			{
			if (baseContextOverriden)
				{
				base.AttachBaseContext (@base);
				return;
				}

			Android.Content.Res.Configuration overrideConfiguration = new Android.Content.Res.Configuration ();
			overrideConfiguration = @base.Resources.Configuration;
			overrideConfiguration.FontScale = 0.9f;

			Context context = @base.CreateConfigurationContext (overrideConfiguration);
			baseContextOverriden = true;

			base.AttachBaseContext (context);
			}
		private bool baseContextOverriden = false;

		/// <summary>
		/// Обработчик события создания экземпляра
		/// </summary>
		protected override void OnCreate (Bundle savedInstanceState)
			{
			// Отмена темы для splash screen
			base.SetTheme (Microsoft.Maui.Controls.Resource.Style.MainTheme);

			// Настройка
			Platform.Init (this, savedInstanceState);

			// Инициализация службы
			if (mainService == null)
				{
				mainService = new Intent (this, typeof (MainService));
				mainService.SetPackage (this.PackageName);
				}
			AndroidSupport.StopRequested = false;

			// Для Android 12 и выше запуск службы возможен только здесь
			RDAppStartupFlags flags = AndroidSupport.GetAppStartupFlags (RDAppStartupFlags.CanShowNotifications);
			if (flags.HasFlag (RDAppStartupFlags.CanShowNotifications))
				{
				if (AndroidSupport.IsForegroundAvailable)
					StartForegroundService (mainService);
				else
					StartService (mainService);
				}

			// Запуск
			if (NotificationsSupport.KeepScreenOn)
				this.Window.AddFlags (WindowManagerFlags.KeepScreenOn);

			base.OnCreate (savedInstanceState);
			}
		private Intent mainService;

		/// <summary>
		/// Остановка службы, если при сворачивании приложения она была отключена
		/// </summary>
		protected override void OnStop ()
			{
			// Запрос на остановку при необходимости
			if (!NotificationsSupport.AllowServiceToStart)
				AndroidSupport.StopRequested = true;
			// Иначе служба продолжит работу в фоне

			base.OnStop ();
			}

		/// <summary>
		/// Перезапуск службы при возвращении в основное приложение
		/// </summary>
		protected override void OnResume ()
			{
			// Перезапуск, если была остановлена (независимо от разрешения)
			if (mainService == null)
				{
				mainService = new Intent (this, typeof (MainService));
				mainService.SetPackage (this.PackageName);
				}
			AndroidSupport.StopRequested = false;

			// Нет смысла запускать сервис, если он не был закрыт приложением.
			// Также функция запуска foreground из свёрнутого состояния недоступна в Android 12 и новее
			if (NotificationsSupport.AllowServiceToStart || !AndroidSupport.IsForegroundStartableFromResumeEvent)
				{
				base.OnResume ();
				return;
				}

			// Повторный запуск службы
			if (AndroidSupport.IsForegroundAvailable)
				StartForegroundService (mainService);
			else
				StartService (mainService);

			base.OnResume ();
			}
		}

	/// <summary>
	/// Класс описывает фоновую службу новостей приложения
	/// </summary>
	[Service (Name = "com.RD_AAOW.UniNotifier",
		ForegroundServiceType = ForegroundService.TypeDataSync,
		Label = "uNot",
		Exported = true)]
	public class MainService: Service
		{
		// Идентификаторы процесса
		private Handler handler;
		private Action runnable;

		// Состояние службы
		private bool isStarted = false;

		// Дескрипторы уведомлений
		private NotificationCompat.Builder notBuilder;
		private NotificationManager notManager;
		private NotificationChannel urgentChannel, defaultChannel;
		private NotificationCompat.BigTextStyle notTextStyle;
		private string urgentChannelID, defaultChannelID;
		private int urgentColor = 0xFF8000, defaultColor = 0x80FFC0;

		// Дескрипторы действий
		private Intent masterIntent;
		private PendingIntent masterPendingIntent;

		// Дескрипторы обработчиков событий
		private BroadcastReceiver[] bcReceivers =
			new BroadcastReceiver[AndroidSupport.IntentFiltersForBootReceiver.Length];

		// Время следующего опроса
		private DateTime nextRequest = new DateTime (2000, 1, 1, 0, 0, 0);

		/// <summary>
		/// Обработчик события создания службы
		/// </summary>
		public override void OnCreate ()
			{
			// Базовая обработка
			base.OnCreate ();

			// Запуск в бэкграунде
			handler = new Handler (Looper.MainLooper);

			// Аналог таймера (создаёт задание, которое само себя ставит в очередь исполнения ОС)
			runnable = new Action (() =>
			{
				if (isStarted)
					{
					TimerTick ();
					handler.PostDelayed (runnable, ProgramDescription.MasterFrameLength);
					}
			});
			}

		// Запрос всех новостей
		private async Task<string> GetNotification ()
			{
			// Оболочка с включённой в неё паузой (иначе блокируется интерфейсный поток)
			Thread.Sleep ((int)ProgramDescription.MasterFrameLength * 2);
			return await ProgramDescription.NSet.GetNextNotification (-1);
			}

		// Основной метод службы
		private async void TimerTick ()
			{
			// Контроль требования завершения службы (игнорирует все прочие флаги)
			if (isStarted && AndroidSupport.StopRequested)
				{
				// Остановка службы
				isStarted = false;

				// Освобождение ресурсов
				notBuilder.Dispose ();
				masterIntent.Dispose ();
				masterPendingIntent.Dispose ();

				foreach (BroadcastReceiver br in bcReceivers)
					this.UnregisterReceiver (br);

				// Глушение (и отправка события destroy)
				StopSelf ();

				return;
				}

			// Защита от двойного входа
			if (NotificationsSupport.BackgroundRequestInProgress)
				return;

			// Отмена действия, если запущено основное приложение
			string msg = "";
			if (AndroidSupport.AppIsRunning)
				{
				// Признак того, что отображено число новых сообщений
				if (newItemsShown)
					{
					newItemsShown = false;
					if (AndroidSupport.IsForegroundStartableFromResumeEvent)
						msg = RDLocale.GetText ("LaunchMessage");
					else
						msg = RDLocale.GetText ("LaunchMessageA13");

					if (AndroidSupport.IsForegroundAvailable)
						notBuilder.SetChannelId (defaultChannelID);
					notBuilder.SetColor (defaultColor);

					goto notMessage;    // inProgress == false
					}

				return;
				}

			// Опрос по достижении даты (позволяет выполнить моментальный опрос при простое службы)
			if (DateTime.Now < nextRequest)
				return;

			// Контроль интернет-подключения (во избежание ложных сообщений о недоступности ресурсов)
			if (Connectivity.NetworkAccess != NetworkAccess.Internet)
				{
				nextRequest = DateTime.Now.AddMinutes (NotificationsSupport.BackgroundRequestStepMinutes);
				return;
				}

			// Перезапрос журнала и счётчика выполняется здесь, т.к. состояние могло измениться в основном интерфейсе
			NotificationsSupport.BackgroundRequestInProgress = true;
			nextRequest = DateTime.Now.AddMinutes (NotificationsSupport.BackgroundRequestStepMinutes);

			List<MainLogItem> masterLog = new List<MainLogItem> (NotificationsSupport.GetMasterLog (false));

			// Разблокировка метода GetHTML
			AndroidSupport.StopRequested = false;

			// Извлечение новых записей
			string newText = "";
			bool haveNews = false;
			while (!AndroidSupport.AppIsRunning && ((newText = await Task<string>.Run (GetNotification)) !=
				NotificationsSet.NoNewsSign))
				if (newText != "")
					{
					if (NotificationsSupport.LogNewsItemsAtTheEnd)
						masterLog.Add (new MainLogItem (newText));
					else
						masterLog.Insert (0, new MainLogItem (newText));

					NotificationsSupport.NewItems++;
					haveNews = true;
					}

			// Сохранение с обрезкой журнала
			NotificationsSupport.SetMasterLog (masterLog);

			// Отсечка на случай пересечения с запуском основного приложения или при отсутствии изменений
			if (AndroidSupport.AppIsRunning || !haveNews)
				{
				NotificationsSupport.BackgroundRequestInProgress = false;
				return;
				}

			// Оповещение пользователя
			msg = (ProgramDescription.NSet.HasUrgentNotifications ?
				RDLocale.GetText ("NewItemsUrgentMessage") : "") +

				string.Format (RDLocale.GetText (AndroidSupport.IsForegroundStartableFromResumeEvent ?
				"NewItemsMessage" : "NewItemsMessageA13"),

				NotificationsSupport.NewItems);
			newItemsShown = true;

			// Дополнительная защита от множественных вызовов с малым интервалом
			if (!isStarted)
				return;

			// Подтягивание настроек из интерфейса
			notBuilder.SetDefaults (!ProgramDescription.NSet.HasUrgentNotifications ? 0 :

				(int)(NotificationsSupport.AllowSound ? NotificationDefaults.Sound : 0) |
				(int)(NotificationsSupport.AllowLight ? NotificationDefaults.Lights : 0) |
				(int)(NotificationsSupport.AllowVibro ? NotificationDefaults.Vibrate : 0));

			if (AndroidSupport.IsForegroundAvailable)
				notBuilder.SetChannelId (!ProgramDescription.NSet.HasUrgentNotifications ?
					defaultChannelID : urgentChannelID);
			notBuilder.SetColor (ProgramDescription.NSet.HasUrgentNotifications ? urgentColor : defaultColor);

			// Формирование сообщения
		notMessage:
			notBuilder.SetContentText (msg);
			notTextStyle.BigText (msg);
			Android.App.Notification notification = notBuilder.Build ();

			// Отображение (с дублированием для срочных)
			notManager.Notify (ProgramDescription.NotServiceID, notification);

			// Завершено
			notification.Dispose ();
			NotificationsSupport.BackgroundRequestInProgress = false;
			}
		private bool newItemsShown = false;

		/// <summary>
		/// Обработчик события запуска службы
		/// </summary>
		public override StartCommandResult OnStartCommand (Intent intent, StartCommandFlags flags, int startId)
			{
			// Защита
			if (isStarted)
				return StartCommandResult.NotSticky;

			// Инициализация оповещений
			if (ProgramDescription.NSet == null)
				ProgramDescription.NSet = new NotificationsSet (false);

			// Инициализация объектов настройки
			notManager = (NotificationManager)this.GetSystemService (Service.NotificationService);
			notBuilder = new NotificationCompat.Builder (this, ProgramDescription.AssemblyMainName.ToLower ());

			// Создание канала (для Android O и выше, поэтому это свойство)
			if (AndroidSupport.IsForegroundAvailable)
				{
				urgentChannelID = ProgramDescription.AssemblyMainName.ToLower () + "_urgent";
				urgentChannel = new NotificationChannel (urgentChannelID,
					ProgramDescription.AssemblyVisibleName + " / Urgent", NotificationImportance.High);
				defaultChannelID = ProgramDescription.AssemblyMainName.ToLower () + "_default";
				defaultChannel = new NotificationChannel (defaultChannelID,
					ProgramDescription.AssemblyVisibleName + " / Non-urgent", NotificationImportance.High);

				// Настройка
				urgentChannel.Description = RDLocale.GetText ("UrgentChannel");
				defaultChannel.Description = RDLocale.GetText ("DefaultChannel");
				urgentChannel.LockscreenVisibility = defaultChannel.LockscreenVisibility =
					NotificationVisibility.Private;

				// Создание
				notManager.CreateNotificationChannel (urgentChannel);
				notManager.CreateNotificationChannel (defaultChannel);

				// Запуск
				notBuilder.SetChannelId (defaultChannelID);
				}

			// Категория "сообщение"
			notBuilder.SetCategory ("msg");

			// Оттенок заголовков оповещений
			notBuilder.SetColor (defaultColor);

			// Android 13 и новее: не позволяет закрыть оповещение вручную (смахиванием)
			notBuilder.SetOngoing (true);

			// Android 12 и новее: требует немедленного отображения оповещения
			if (!AndroidSupport.IsForegroundStartableFromResumeEvent)
				notBuilder.SetForegroundServiceBehavior (NotificationCompat.ForegroundServiceImmediate);

			string launchMessage;
			if (AndroidSupport.IsForegroundStartableFromResumeEvent)
				launchMessage = RDLocale.GetText ("LaunchMessage");
			else
				launchMessage = RDLocale.GetText ("LaunchMessageA13");

			notBuilder.SetContentText (launchMessage);
			notBuilder.SetContentTitle (ProgramDescription.AssemblyVisibleName);
			notBuilder.SetTicker (ProgramDescription.AssemblyVisibleName);

			// Для служебного сообщения
			notBuilder.SetDefaults (0);

			// Настройка видимости для стартового сообщения
			if (!AndroidSupport.IsForegroundAvailable)
				notBuilder.SetPriority ((int)NotificationPriority.Default);
			else
				notBuilder.SetPriority ((int)NotificationPriority.High);

			notBuilder.SetSmallIcon (Resource.Drawable.ic_not);
			if (AndroidSupport.IsLargeIconRequired)
				notBuilder.SetLargeIcon (BitmapFactory.DecodeResource (this.Resources, Resource.Drawable.ic_not_large));
			notBuilder.SetVisibility ((int)NotificationVisibility.Public);

			notTextStyle = new NotificationCompat.BigTextStyle (notBuilder);
			notTextStyle.BigText (launchMessage);

			// Прикрепление ссылки для перехода в основное приложение
			masterIntent = new Intent (this, typeof (NotificationLink));
			masterIntent.SetPackage (this.PackageName);

			masterPendingIntent = PendingIntent.GetService (this, ProgramDescription.NotServiceID,
				masterIntent, PendingIntentFlags.Immutable);
			notBuilder.SetContentIntent (masterPendingIntent);

			// Стартовое сообщение (с приведением к требованиям к Android 14)
			Android.App.Notification notification = notBuilder.Build ();
			StartForeground (ProgramDescription.NotServiceID, notification, ForegroundService.TypeDataSync);

			// Перенастройка для основного режима
			if (!AndroidSupport.IsForegroundAvailable)
				{
				notBuilder.SetDefaults (0);
				notBuilder.SetPriority ((int)NotificationPriority.Max);
				}

			// Регистрация ресиверов событий перезагрузки
			for (int i = 0; i < AndroidSupport.IntentFiltersForBootReceiver.Length; i++)
				{
				bcReceivers[i] = new BootReceiver ();
				this.RegisterReceiver (bcReceivers[i], new IntentFilter (AndroidSupport.IntentFiltersForBootReceiver[i]),
					ReceiverFlags.Exported);
				}

			// Запуск петли
			handler.PostDelayed (runnable, ProgramDescription.MasterFrameLength);
			isStarted = true;

			return StartCommandResult.NotSticky;
			}

		/// <summary>
		/// Обработчик остановки службы
		/// </summary>
		public override void OnDestroy ()
			{
			// Освобождение ресурсов, которые нельзя освободить в таймере
			handler.RemoveCallbacks (runnable);
			notManager.Cancel (ProgramDescription.NotServiceID);
			if (AndroidSupport.IsForegroundAvailable)
				{
				notManager.DeleteNotificationChannel (urgentChannelID);
				notManager.DeleteNotificationChannel (defaultChannelID);
				}

			notManager.Dispose ();

			// Остановка
			if (AndroidSupport.IsForegroundAvailable)
				StopForeground (StopForegroundFlags.Remove);
			else
				StopForeground (true);
			StopSelf ();

			// Стандартная обработка
			base.OnDestroy ();
			}

		/// <summary>
		/// Обработчик привязки службы (заглушка)
		/// </summary>
		public override IBinder OnBind (Intent intent)
			{
			// Return null because this is a pure started service
			return null;
			}

		/// <summary>
		/// Обработчик события снятия задачи (заглушка)
		/// </summary>
		public override void OnTaskRemoved (Intent rootIntent)
			{
			}
		}

	/// <summary>
	/// Класс описывает задание на открытие веб-ссылки
	/// </summary>
	[Service (Name = "com.RD_AAOW.UniNotifierLink",
		Label = "UniNotifierLink",
		ForegroundServiceType = ForegroundService.TypeDataSync,
		Exported = true)]
	public class NotificationLink: JobIntentService
		{
		/// <summary>
		/// Конструктор (заглушка)
		/// </summary>
		public NotificationLink ()
			{
			}

		/// <summary>
		/// Обработка события выполнения задания для Android O и новее
		/// </summary>
		public override StartCommandResult OnStartCommand (Intent intent, StartCommandFlags flags, int startId)
			{
			if (AndroidSupport.IsForegroundAvailable)
				StartMasterActivity ();

			return base.OnStartCommand (intent, flags, startId);
			}

		/// <summary>
		/// Обработка события выполнения задания для Android N и старше
		/// </summary>
		protected override void OnHandleWork (Intent intent)
			{
			if (!AndroidSupport.IsForegroundAvailable)
				StartMasterActivity ();
			}

		// Общий метод запуска
		private void StartMasterActivity ()
			{
			if (AndroidSupport.AppIsRunning)
				return;
			AndroidSupport.StopRequested = false;

			if (mainActivity == null)
				{
				mainActivity = new Intent (this, typeof (MainActivity));
				mainActivity.SetPackage (this.PackageName);
				}

			// Требование Android 12
			PendingIntent.GetActivity (this, ProgramDescription.NotServiceID, mainActivity,
				PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable).Send ();
			}
		private Intent mainActivity;
		}

	/// <summary>
	/// Класс описывает приёмник события окончания загрузки ОС
	/// </summary>
	[BroadcastReceiver (Name = "com.RD_AAOW.UniNotifierBoot",
		Label = "UniNotifierBoot",
		Exported = true)]
	public class BootReceiver: BroadcastReceiver
		{
		/// <summary>
		/// Обработчик события наступления события окончания загрузки
		/// </summary>
		public override void OnReceive (Context context, Intent intent)
			{
			if (!NotificationsSupport.AllowServiceToStart || (intent == null))
				return;

			bool received = false;
			for (int i = 0; i < AndroidSupport.IntentFiltersForBootReceiver.Length; i++)
				if (intent.Action.Equals (AndroidSupport.IntentFiltersForBootReceiver[i],
					StringComparison.CurrentCultureIgnoreCase))
					{
					received = true;
					break;
					}

			if (received)
				{
				if (mainService == null)
					{
					mainService = new Intent (context, typeof (MainService));
					mainService.SetPackage (AppInfo.PackageName);
					mainService.AddFlags (ActivityFlags.NewTask);
					mainService.AddFlags (ActivityFlags.ResetTaskIfNeeded);
					}
				AndroidSupport.StopRequested = false;

				// Для Android 12 и выше запуск службы возможен только здесь
				RDAppStartupFlags flags = AndroidSupport.GetAppStartupFlags (RDAppStartupFlags.CanShowNotifications);
				if (flags.HasFlag (RDAppStartupFlags.CanShowNotifications))
					{
					if (AndroidSupport.IsForegroundAvailable)
						context.StartForegroundService (mainService);
					else
						context.StartService (mainService);
					}
				}
			}
		private Intent mainService;
		}
	}
