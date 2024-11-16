﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает главную форму приложения
	/// </summary>
	public partial class UniNotifierForm: Form
		{
		// Переменные

		// Иконка в трее системы
		private NotifyIcon ni = new NotifyIcon ();

		// Флаг разрешения на выход из приложения без сворачивания в трей
		private bool allowExit = false;

		// Набор сформированных уведомлений
		private NotificationsSet ns = new NotificationsSet (true);

		// Тексты оповещений
		private List<string> texts = new List<string> ();

		// Индексы оповещений-источников в порядке, в котором они поступили в первичный стек
		private List<int> notNumbers = new List<int> ();

		// Индексы оповещений-источников в порядке, в котором они расположены в журнале
		private List<int> notSenders = new List<int> ();

		// Флаг, указывающий на необходимость сворачивания окна в трей
		private bool hideWindow;

		// Контекстное меню со списком оповещений для ручного перехода
		private ContextMenu notContextMenu;

		// Контекстное меню элемента журнала
		private ContextMenu textContextMenu;

		// Индекс элемента журнала, отправившего запрос на отображение меню
		private int textContextSender;

		// Семейство шрифтов для журнала
		private const string fontFamily = "Calibri";

		// Коэффициент непрозрачности
		private const int transculencyAmount = 15;

#if TGT
		// Флаги трассировщика GMJ
		private bool tgtInProgress = false;
		private uint tgtCounter = 0;
#endif

		// Возвращает динамический внешний отступ элементов журнала
		private Padding LogItemMargin
			{
			get
				{
				return new Padding (3, 3, 3, (int)NotificationsSupport.LogFontSize /
					(NotificationsSupport.TranslucentLogItems ? 8 : 4));
				}
			}

		// Возвращает ограничение ширины поля журнала
		private Size LogSizeLimit
			{
			get
				{
				return new Size (MainLayout.Width - 6 - 18, 0);
				}
			}

		/// <summary>
		/// Конструктор. Настраивает главную форму приложения
		/// </summary>
		public UniNotifierForm (bool HideWindow)
			{
			// Инициализация
			InitializeComponent ();

			this.Text = ProgramDescription.AssemblyVisibleName;
			this.CancelButton = BClose;

			if (!RDGenerics.AppHasAccessRights (false, false))
				this.Text += RDLocale.GetDefaultText (RDLDefaultTexts.Message_LimitedFunctionality);
			hideWindow = HideWindow;

			// Получение настроек
			ReloadNotificationsList ();
			ApplyLogSettings ();

			RDGenerics.LoadWindowDimensions (this);

			// Настройка иконки в трее
			ni.Icon = Properties.UniNotifier.UniNotifierTrayN;
			ni.Text = ProgramDescription.AssemblyVisibleName;
			ni.Visible = true;
			ReloadTrayContextMenu ();
			}

		private void UniNotifierForm_Shown (object sender, EventArgs e)
			{
			// Скрытие окна настроек
			UniNotifierForm_Resize (null, null);
			if (hideWindow)
				this.Hide ();

			// Запуск
			MainTimer.Interval = (int)ProgramDescription.MasterFrameLength * 4;
			MainTimer.Enabled = true;

#if TGB
			GetGMJ_Click (null, null);
#endif
			}

		// Обновление списка оповещений в главном окне
		private void ReloadNotificationsList ()
			{
			// Локализация зависимой части интерфейса
			BGo.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_GoTo);

			if (textContextMenu == null)
				textContextMenu = new ContextMenu ();

			textContextMenu.MenuItems.Clear ();
			textContextMenu.MenuItems.Add (new MenuItem (BGo.Text,
				TextContext_ItemClicked));
			textContextMenu.MenuItems.Add (new MenuItem (RDLocale.GetDefaultText (RDLDefaultTexts.Button_Copy),
				TextContext_ItemClicked));
			textContextMenu.MenuItems.Add (new MenuItem ("-"));
			textContextMenu.MenuItems.Add (new MenuItem (RDLocale.GetText ("DeleteContextMenu"),
				TextContext_ItemClicked));
			textContextMenu.MenuItems.Add (new MenuItem (RDLocale.GetText ("DisableContextMenu"),
				TextContext_ItemClicked));
			textContextMenu.MenuItems.Add (new MenuItem (RDLocale.GetText ("SetupContextMenu"),
				TextContext_ItemClicked));

			// Перезагрузка списка уведомлений
			if (notContextMenu == null)
				notContextMenu = new ContextMenu ();

			notContextMenu.MenuItems.Clear ();
			foreach (Notification n in ns.Notifications)
				notContextMenu.MenuItems.Add (new MenuItem (n.Name, GoToLink_ItemClicked));
			}

		// Обновление меню иконки в трее
		private void ReloadTrayContextMenu ()
			{
			bool create = false;
			if (ni.ContextMenu == null)
				{
				ni.ContextMenu = new ContextMenu ();
				create = true;
				}

			// Только локализация
			if (!create)
				{
				ni.ContextMenu.MenuItems[0].Text = RDLocale.GetText ("MainMenuOption02");
				ni.ContextMenu.MenuItems[1].Text = RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout);
				ni.ContextMenu.MenuItems[2].Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Exit);

				return;
				}

			// Создание
			ni.ContextMenu.MenuItems.Add (new MenuItem (RDLocale.GetText ("MainMenuOption02"), ShowSettings));
			ni.ContextMenu.MenuItems[0].Enabled = RDGenerics.AppHasAccessRights (false, true);

			ni.ContextMenu.MenuItems.Add (new MenuItem (
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout), AboutService));
			ni.ContextMenu.MenuItems.Add (new MenuItem (
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Exit), CloseService));

			ni.MouseDown += ShowHideFullText;
			ni.ContextMenu.MenuItems[2].DefaultItem = true;

			return;
			}

		// Завершение работы службы
		private void CloseService (object sender, EventArgs e)
			{
			allowExit = true;
			this.Close ();
			}

		private void UniNotifierForm_FormClosing (object sender, FormClosingEventArgs e)
			{
			// Остановка службы
			if (allowExit)
				{
				// Остановка
				ni.Visible = false;
				MainTimer.Enabled = false;

				// Освобождение ресурсов
				ns.Dispose ();
				}

			// Скрытие окна просмотра
			else
				{
				this.Hide ();
				e.Cancel = true;
				}
			}

		// О приложении
		private void AboutService (object sender, EventArgs e)
			{
			RDGenerics.ShowAbout (false);
			}

		// Итерация таймера обновления
		private void MainTimer_Tick (object sender, EventArgs e)
			{
			// Переменные
			int spl;
			string hdr, txt;

#if TGT
			if (tgtInProgress)
				return;
#endif

			// Запуск запроса
			RDGenerics.RunWork (DoUpdate, null, null, RDRunWorkFlags.DontSuspendExecution);

#if TGT
			// Раз в 13 минут (1000 * 60 * 13)
			if (++tgtCounter * MainTimer.Interval >= 780000)
				{
				tgtInProgress = true;
				tgtCounter = 0;

				GetGMJ_Click (null, null);
				tgtInProgress = false;
				}
#endif

			// Обновление очереди отображения
			if (texts.Count > 0)
				{
				// Добавление и форматирование
				AddTextToLayout (texts[0].Replace (NotificationsSupport.MainLogItemSplitter.ToString (),
					RDLocale.RN));

				// Отображение всплывающего сообщения
				if (!this.Visible)
					{
					try
						{
						spl = texts[0].IndexOf (NotificationsSupport.MainLogItemSplitter);
						hdr = texts[0].Substring (0, spl);
						txt = texts[0].Substring (spl + 1);

						if (txt.Length > 210)
							txt = txt.Substring (0, 210) + "...";

						if (ns.HasUrgentNotifications)
							{
							ni.ShowBalloonTip (10000, hdr, txt, ToolTipIcon.Warning);
							ni.Icon = Properties.UniNotifier.UniNotifierTrayW;
							}
						else
							{
							ni.ShowBalloonTip (10000, hdr, txt, ToolTipIcon.Info);
							ni.Icon = Properties.UniNotifier.UniNotifierTrayN;
							}
						}
					catch { }
					}

				texts.RemoveAt (0);
				notSenders.Add (notNumbers[0]);
				notNumbers.RemoveAt (0);
				}

			// Срочные оповещения
			if (ns.HasUrgentNotifications && NotificationsSupport.CallWindowOnUrgents)
				{
				SetUrgency (false);

				this.Show ();
				this.TopMost = true;
				this.TopMost = false;
				}
			}

		private void DoUpdate (object sender, DoWorkEventArgs e)
			{
			string newText = ns.GetNextNotification ();
			if (newText != "")
				{
				texts.Add (newText);
				notNumbers.Add (ns.CurrentNotificationNumberInList);
				}

			e.Result = null;
			}

		// Метод устанавливает новое состояние срочности для набора уведомлений
		private void SetUrgency (bool Urgent)
			{
			ns.HasUrgentNotifications = Urgent;
			try
				{
				ni.Icon = Urgent ? Properties.UniNotifier.UniNotifierTrayW : Properties.UniNotifier.UniNotifierTrayN;
				}
			catch { }
			}

		// Отображение / скрытие полного списка оповещений
		private void ShowHideFullText (object sender, MouseEventArgs e)
			{
			// Работа только с левой кнопкой мыши
			if (e.Button != MouseButtons.Left)
				return;

			// Отмена состояния сообщений
			SetUrgency (false);

			// Обработка состояния
			if (this.Visible)
				{
				this.Close ();
				}
			else
				{
				this.Show ();
				this.TopMost = true;
				this.TopMost = false;
				ScrollLog ();
				}
			}

		// Метод прокручивает журнал к последней записи
		private void ScrollLog ()
			{
			if (MainLayout.Controls.Count > 0)
				MainLayout.ScrollControlIntoView (MainLayout.Controls[MainLayout.Controls.Count - 1]);
			}

		// Отображение окна настроек
		private void ShowSettings (object sender, EventArgs e)
			{
			// Пауза
			MainTimer.Enabled = false;

			// Настройка
			UNGenSettingsForm sf = new UNGenSettingsForm (ns, (uint)MainTimer.Interval *
				NotificationsSet.MaxNotifications / 60000,
				sender == null ? notSenders[textContextSender] : -1);

			// Запоминание настроек
			bool complete = sf.CompleteUpdate;
			sf.Dispose ();

			// Обработка случая закрытия основного окна из трея
			if (allowExit)
				return;

			// Обновление настроек
			ReloadNotificationsList ();
			SetUrgency (false);

			ReloadTrayContextMenu ();
			ApplyLogSettings ();

			// Перезапуск
			ns.ResetTimer (complete);
			MainTimer.Enabled = true;
			}

		// Применение настроек журнала
		private void ApplyLogSettings ()
			{
			MainLayout.BackColor = NotificationsSupport.LogColors.CurrentColor.BackColor;
			Font fnt = new Font (fontFamily, NotificationsSupport.LogFontSize / 10.0f);
			for (int i = 0; i < MainLayout.Controls.Count; i++)
				{
				Label l = (Label)MainLayout.Controls[i];

				int amount = NotificationsSupport.TranslucentLogItems ? transculencyAmount : 0;
				if (NotificationsSupport.LogColors.CurrentColor.IsBright)
					l.BackColor = Color.FromArgb (amount, 0, 0, 0);
				else
					l.BackColor = Color.FromArgb (amount, 255, 255, 255);

				l.ForeColor = NotificationsSupport.LogColors.CurrentColor.MainTextColor;

				l.Font = fnt;
				l.Margin = LogItemMargin;
				}
			}

		// Переход на страницу сообщества
		private void GoToLink (object sender, EventArgs e)
			{
			ProgramDescription.ShowTip (NSTipTypes.GoToButton);
			notContextMenu.Show (BGo, Point.Empty);
			}

		private void GoToLink_ItemClicked (object sender, EventArgs e)
			{
			int idx = notContextMenu.MenuItems.IndexOf ((MenuItem)sender);
			if (idx >= 0)
				RDGenerics.RunURL (ns.Notifications[idx].Link);

			this.Close ();
			}

		// Закрытие окна просмотра
		private void BClose_Click (object sender, EventArgs e)
			{
			// Отмена состояния сообщений
			SetUrgency (false);

			this.Close ();
			}

		// Изменение размера формы
		private void UniNotifierForm_Resize (object sender, EventArgs e)
			{
			MainLayout.Width = this.Width - 38;
			MainLayout.Height = this.Height - ButtonsPanel.Height - 53;

			ButtonsPanel.Top = MainLayout.Top + MainLayout.Height + 1;
			ButtonsPanel.Left = (this.Width - ButtonsPanel.Width) / 2;
			}

		// Сохранение размера формы
		private void UniNotifierForm_ResizeEnd (object sender, EventArgs e)
			{
			// Сохранение настроек
			RDGenerics.SaveWindowDimensions (this);

			// Пересчёт размеров элементов
			for (int i = 0; i < MainLayout.Controls.Count; i++)
				{
				Label l = (Label)MainLayout.Controls[i];
				l.AutoSize = false;
				l.MaximumSize = l.MinimumSize = LogSizeLimit;
				l.AutoSize = true;
				}
			}

		// Запрос сообщения от GMJ
		private void GetGMJ_Click (object sender, EventArgs e)
			{
#if TGB || TGT
			string s = GMJ.GetRandomGMJ ();

			if (s != "")
				{
#if TGT
				if (s.Contains (GMJ.SourceNoReturnPattern))
					{
					texts.Add ("!!! " + s + " !!!");
					SetUrgency (true);
					}
				else
					{
					texts.Add (s);
					}
#else
				texts.Add (s);
#endif
				}
			else
				{
#if TGB
				texts.Add ("Новый список сформирован");
#else
				texts.Add ("GMJ не отвечает на запрос. Проверьте интернет-соединение");
#endif
				}
			notNumbers.Add (0);

#if TGB
			ni.ShowBalloonTip (10000, ProgramDescription.AssemblyVisibleName,
				texts[texts.Count - 1], ToolTipIcon.Info);
			CloseService (null, null);
#endif
#endif
			}

		// Метод добавляет этемент в MainLayout
		private void AddTextToLayout (string Text)
			{
			// Формирование контрола
			Label l = new Label ();
			l.AutoSize = false;

			if (Text.Contains (NotificationsSet.EmergencySign))
				{
				l.BackColor = RDGenerics.GetInterfaceColor (RDInterfaceColors.WarningMessage);
				l.ForeColor = RDGenerics.GetInterfaceColor (RDInterfaceColors.DefaultText);

				if (NotificationsSupport.LogColors.CurrentColor.IsBright)
					l.BorderStyle = BorderStyle.FixedSingle;
				}
			else
				{
				l.ForeColor = NotificationsSupport.LogColors.CurrentColor.MainTextColor;

				int amount = NotificationsSupport.TranslucentLogItems ? transculencyAmount : 0;
				if (NotificationsSupport.LogColors.CurrentColor.IsBright)
					l.BackColor = Color.FromArgb (amount, 0, 0, 0);
				else
					l.BackColor = Color.FromArgb (amount, 255, 255, 255);
				}

			/*l.Click += TextLabel_Clicked;*/
			l.MouseClick += TextContext_MouseClick;
			l.Font = new Font (fontFamily, NotificationsSupport.LogFontSize / 10.0f);
			l.Text = Text;
			l.Margin = LogItemMargin;

			l.MaximumSize = l.MinimumSize = LogSizeLimit;
			l.AutoSize = true;

			// Добавление
			MainLayout.Controls.Add (l);

			while (MainLayout.Controls.Count > NotificationsSupport.MasterLogMaxItems)
				{
				MainLayout.Controls.RemoveAt (0);
				notSenders.RemoveAt (0);
				}

			// Прокрутка
			ScrollLog ();
			}

		/*// Нажатие на элемент журнала
		private void TextLabel_Clicked (object sender, EventArgs e)
			{
			Label l = (Label)sender;
			textContextSender = MainLayout.Controls.IndexOf (l);

			textContextMenu.Show (l, Point.Empty);
			}*/

		// Выбор варианта в меню
		private void TextContext_ItemClicked (object sender, EventArgs e)
			{
			int idx = textContextMenu.MenuItems.IndexOf ((MenuItem)sender);
			if (textContextSender < 0)
				return;

			switch (idx)
				{
				// Переход по ссылке
				case 0:
					RDGenerics.RunURL (ns.Notifications[notSenders[textContextSender]].Link);
					this.Close ();
					break;

				// Копирование текста
				case 1:
					RDGenerics.SendToClipboard (((Label)MainLayout.Controls[textContextSender]).Text +
						RDLocale.RNRN + ns.Notifications[notSenders[textContextSender]].Link, true);
					break;

				// Удаление текста
				case 3:
					MainLayout.Controls.RemoveAt (textContextSender);
					notSenders.RemoveAt (textContextSender);
					break;

				// Отключение уведомления
				case 4:
					ns.Notifications[notSenders[textContextSender]].IsEnabled = false;
					break;

				// Настройка уведомления
				case 5:
					ShowSettings (null, null);
					break;
				}
			}

		// Нажатие на элемент журнала
		private void TextContext_MouseClick (object sender, MouseEventArgs e)
			{
			Label l = (Label)sender;
			textContextSender = MainLayout.Controls.IndexOf (l);

			textContextMenu.Show (l, e.Location);
			}
		}
	}
