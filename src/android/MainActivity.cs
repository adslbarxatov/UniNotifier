using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.App;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#if DEBUG
[assembly: Application (Debuggable = true)]
#else
[assembly: Application (Debuggable = false)]
#endif

namespace RD_AAOW.Droid
	{
	/// <summary>
	/// Класс описывает загрузчик приложения
	/// </summary>
	[Activity (Label = "uNot", Icon = "@mipmap/icon", Theme = "@style/MainTheme",
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, Exported = true)]
	public class MainActivity:global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
		{
		/// <summary>
		/// Обработчик события создания экземпляра
		/// </summary>
		protected override void OnCreate (Bundle savedInstanceState)
			{
			// Базовая настройка
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			base.OnCreate (savedInstanceState);
			global::Xamarin.Forms.Forms.Init (this, savedInstanceState);

			// Запуск независимо от разрешения
			Intent mainService = new Intent (this, typeof (MainService));
			AndroidSupport.StopRequested = false;

			if (AndroidSupport.IsForegroundAvailable)
				StartForegroundService (mainService);
			else
				StartService (mainService);

			// Запуск
			LoadApplication (new App ());
			}

		/// <summary>
		/// Перезапуск службы
		/// </summary>
		protected override void OnStop ()
			{
			Intent mainService = new Intent (this, typeof (MainService));

			// Запрос на остановку при необходимости
			if (!AndroidSupport.AllowServiceToStart)
				AndroidSupport.StopRequested = true;
			// Иначе служба продолжит работу в фоне

			base.OnStop ();
			}

		/// <summary>
		/// Перезапуск основного приложения
		/// </summary>
		protected override void OnResume ()
			{
			// Перезапуск, если была остановлена (независимо от разрешения)
			Intent mainService = new Intent (this, typeof (MainService));
			AndroidSupport.StopRequested = false;

			if (AndroidSupport.IsForegroundAvailable)
				StartForegroundService (mainService);
			else
				StartService (mainService);

			base.OnResume ();
			}
		}

	/// <summary>
	/// Класс описывает экран-заставку приложения
	/// </summary>
	[Activity (Theme = "@style/SplashTheme", MainLauncher = true, NoHistory = true,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class SplashActivity:global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
		{
		/// <summary>
		/// Обработчик события создания экземпляра
		/// </summary>
		protected override void OnCreate (Bundle savedInstanceState)
			{
			base.OnCreate (savedInstanceState);
			}

		/// <summary>
		/// Отключение кнопки возврата
		/// </summary>
		public override void OnBackPressed ()
			{
			}

		/// <summary>
		/// Запуск основного действия после завершения загрузки
		/// </summary>
		protected override void OnResume ()
			{
			base.OnResume ();

			Task startup = new Task (() =>
				{
					StartActivity (new Intent (Application.Context, typeof (MainActivity)));
				});
			startup.Start ();
			}
		}

	/// <summary>
	/// Класс описывает фоновую службу новостей приложения
	/// </summary>
	[Service (Name = "com.RD_AAOW.UniNotifier", Label = "uNot",
		Icon = "@mipmap/icon", Exported = true)]
	public class MainService:global::Android.App.Service
		{
		// Переменные и константы
		private Handler handler;                                        // Идентификаторы процесса
		private Action runnable;

		private bool isStarted = false;                                 // Состояние службы

		private NotificationCompat.Builder notBuilder;                  // Дескрипторы уведомлений
		private NotificationManager notManager;
		private const int notServiceID = 4415;
		private NotificationCompat.BigTextStyle notTextStyle;

		private Intent masterIntent;                                    // Дескрипторы действий
		private PendingIntent masterPendingIntent;

		private BroadcastReceiver[] bcReceivers = new BroadcastReceiver[2]; // Дескрипторы обработчиков событий

		private DateTime nextRequest = new DateTime (2000, 1, 1, 0, 0, 0);  // Время следующего опроса

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
			if (AndroidSupport.StopRequested)
				{
				Intent mainService = new Intent (this, typeof (MainService));
				StopService (mainService);

				return;
				}

			// Отмена действия, если таймер отключён
			if (NotificationsSupport.BackgroundRequestStep < 1)
				return;

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
					msg = Localization.GetText ("LaunchMessage", Localization.CurrentLanguage); // Перезапрос языка из приложения
					goto notMessage;    // inProgress == false
					}

				return;
				}

			// Опрос по достижении даты (позволяет выполнить моментальный опрос при простое службы)
			if (DateTime.Now < nextRequest)
				return;

			// Перезапрос журнала и счётчика выполняется здесь, т.к. состояние могло измениться в основном интерфейсе
			NotificationsSupport.BackgroundRequestInProgress = true;
			nextRequest = DateTime.Now.AddMinutes (NotificationsSupport.BackgroundRequestStep *
				NotificationsSupport.BackgroundRequestStepMinutes);

			uint newItems = NotificationsSupport.NewItems;
			List<MainLogItem> masterLog = new List<MainLogItem> (NotificationsSupport.MasterLog);

			// Извлечение новых записей
			AndroidSupport.StopRequested = false;           // Разблокировка метода GetHTML
			ProgramDescription.NSet.ResetTimer (false);     // Без сброса текстов

			string newText = "";
			bool haveNews = false;
			while (!AndroidSupport.AppIsRunning && ((newText = await Task<string>.Run (GetNotification)) !=
				NotificationsSet.NoNewsSign))
				if (newText != "")
					{
					masterLog.Insert (0, new MainLogItem (newText));
					newItems++;
					haveNews = true;
					}

			// Сохранение
			NotificationsSupport.MasterLog = masterLog;
			NotificationsSupport.NewItems = newItems;

			// Отсечка на случай пересечения с запуском основного приложения или при отсутствии изменений
			if (AndroidSupport.AppIsRunning || !haveNews)
				{
				NotificationsSupport.BackgroundRequestInProgress = false;
				return;
				}

			// Оповещение пользователя
			msg = (ProgramDescription.NSet.HasUrgentNotifications ?
				Localization.GetText ("NewItemsUrgentMessage", Localization.CurrentLanguage) : "") +

				string.Format (Localization.GetText ("NewItemsMessage", Localization.CurrentLanguage),
				NotificationsSupport.NewItems);
			newItemsShown = true;

			// Подтягивание настроек из интерфейса
			if (AndroidSupport.AreNotificationsConfigurable)
				notBuilder.SetDefaults ((int)(NotificationsSupport.AllowSound ? NotificationDefaults.Sound : 0) |
					(int)(NotificationsSupport.AllowLight ? NotificationDefaults.Lights : 0) |
					(int)(NotificationsSupport.AllowVibro ? NotificationDefaults.Vibrate : 0));

// Формирование сообщения
notMessage:
			notBuilder.SetContentText (msg);
			notTextStyle.BigText (msg);

			Android.App.Notification notification = notBuilder.Build ();
			notManager.Notify (notServiceID, notification);
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
				NotificationChannel channel = new NotificationChannel (ProgramDescription.AssemblyMainName.ToLower (),
					ProgramDescription.AssemblyMainName, NotificationImportance.High);

				// Настройка
				channel.Description = ProgramDescription.AssemblyTitle;
				channel.LockscreenVisibility = NotificationVisibility.Public;

				// Запуск
				notManager.CreateNotificationChannel (channel);
				notBuilder.SetChannelId (ProgramDescription.AssemblyMainName.ToLower ());
				}

			// Инициализация сообщений
			notBuilder.SetCategory ("msg");     // Категория "сообщение"
			notBuilder.SetColor (0x80FFC0);     // Оттенок заголовков оповещений

			string launchMessage = Localization.GetText ("LaunchMessage", Localization.CurrentLanguage);
			notBuilder.SetContentText (launchMessage);
			notBuilder.SetContentTitle (ProgramDescription.AssemblyVisibleName);
			notBuilder.SetTicker (ProgramDescription.AssemblyVisibleName);

			// Настройка видимости для стартового сообщения
			if (!AndroidSupport.IsForegroundAvailable)
				{
				notBuilder.SetDefaults (0);         // Для служебного сообщения
				notBuilder.SetPriority ((int)NotificationPriority.Default);
				}
			else
				{
				notBuilder.SetDefaults ((int)(NotificationDefaults.Sound | NotificationDefaults.Lights |
					NotificationDefaults.Vibrate)); // Управляется из ОС, но должно быть включено
				notBuilder.SetPriority ((int)NotificationPriority.High);
				}

			notBuilder.SetSmallIcon (Resource.Drawable.ic_not);
			if (AndroidSupport.IsLargeIconRequired)
				notBuilder.SetLargeIcon (BitmapFactory.DecodeResource (this.Resources, Resource.Drawable.ic_not_large));
			notBuilder.SetVisibility ((int)NotificationVisibility.Public);

			notTextStyle = new NotificationCompat.BigTextStyle (notBuilder);
			notTextStyle.BigText (launchMessage);

			// Прикрепление ссылки для перехода в основное приложение
			masterIntent = new Intent (this, typeof (NotificationLink));
			masterPendingIntent = PendingIntent.GetService (this, 0, masterIntent, 0);
			notBuilder.SetContentIntent (masterPendingIntent);

			// Стартовое сообщение
			Android.App.Notification notification = notBuilder.Build ();
			StartForeground (notServiceID, notification);

			// Перенастройка для основного режима
			if (!AndroidSupport.IsForegroundAvailable)
				{
				notBuilder.SetDefaults ((int)(NotificationsSupport.AllowSound ? NotificationDefaults.Sound : 0) |
					(int)(NotificationsSupport.AllowLight ? NotificationDefaults.Lights : 0) |
					(int)(NotificationsSupport.AllowVibro ? NotificationDefaults.Vibrate : 0));
				notBuilder.SetPriority ((int)NotificationPriority.Max);
				}

			// Регистрация ресиверов событий перезагрузки
			this.RegisterReceiver (bcReceivers[0] = new BootReceiver (),
				new IntentFilter (Intent.ActionBootCompleted));
			this.RegisterReceiver (bcReceivers[1] = new BootReceiver (),
				new IntentFilter ("android.intent.action.QUICKBOOT_POWERON"));

			/*// Запрос разрешения на игнорирование оптимизации батареи
			if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
				{
				Intent intentBatRq = new Intent ();
				string packageName = this.PackageName;
				PowerManager pm = (PowerManager)this.GetSystemService (Context.PowerService);

				if (pm.IsIgnoringBatteryOptimizations (packageName))
					{
					intentBatRq.SetAction (Android.Provider.Settings.ActionIgnoreBatteryOptimizationSettings);
					}
				else
					{
					intentBatRq.SetAction (Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
					intentBatRq.SetData (Android.Net.Uri.Parse ("package:" + packageName));
					}

				StartActivity (intentBatRq);
				}*/

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
			// Остановка службы
			handler.RemoveCallbacks (runnable);
			notManager.Cancel (notServiceID);
			if (AndroidSupport.IsForegroundAvailable)
				notManager.DeleteNotificationChannel (ProgramDescription.AssemblyMainName.ToLower ());
			isStarted = false;

			// Освобождение ресурсов
			notBuilder.Dispose ();
			notManager.Dispose ();

			masterIntent.Dispose ();
			masterPendingIntent.Dispose ();

			// Глушение
			if (AndroidSupport.IsForegroundAvailable)
				StopForeground (StopForegroundFlags.Remove);
			else
				StopForeground (true);
			StopSelf ();

			foreach (BroadcastReceiver br in bcReceivers)
				this.UnregisterReceiver (br);

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
	[Service (Name = "com.RD_AAOW.UniNotifierLink", Label = "UniNotifierLink",
		Icon = "@mipmap/icon", Exported = true)]
	public class NotificationLink:JobIntentService
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
				StartActivity ();

			return base.OnStartCommand (intent, flags, startId);
			}

		/// <summary>
		/// Обработка события выполнения задания для Android N и старше
		/// </summary>
		protected override void OnHandleWork (Intent intent)
			{
			if (!AndroidSupport.IsForegroundAvailable)
				StartActivity ();
			}

		// Общий метод запуска
		private void StartActivity ()
			{
			if (AndroidSupport.AppIsRunning)
				return;

			AndroidSupport.StopRequested = false;
			Intent mainActivity = new Intent (this, typeof (MainActivity));
			mainActivity.PutExtra ("Tab", 0);
			PendingIntent.GetActivity (this, 0, mainActivity, PendingIntentFlags.UpdateCurrent).Send ();
			}
		}

	/// <summary>
	/// Класс описывает приёмник события окончания загрузки ОС
	/// </summary>
	[BroadcastReceiver (Name = "com.RD_AAOW.UniNotifierBoot", Label = "UniNotifierBoot",
		Icon = "@mipmap/icon", Exported = true)]
	public class BootReceiver:BroadcastReceiver
		{
		/// <summary>
		/// Обработчик события наступления события окончания загрузки
		/// </summary>
		public override void OnReceive (Context context, Intent intent)
			{
			if (!AndroidSupport.AllowServiceToStart || (intent == null))
				return;

			if (intent.Action.Equals (Intent.ActionBootCompleted, StringComparison.CurrentCultureIgnoreCase) ||
				intent.Action.Equals (Intent.ActionReboot, StringComparison.CurrentCultureIgnoreCase))
				{
				Intent mainService = new Intent (context, typeof (MainService));
				AndroidSupport.StopRequested = false;

				if (AndroidSupport.IsForegroundAvailable)
					context.StartForegroundService (mainService);
				else
					context.StartService (mainService);
				}
			}
		}
	}
