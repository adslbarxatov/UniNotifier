using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает форму настроек оповещений
	/// </summary>
	public partial class UNGenSettingsForm: Form
		{
		// Переменные и константы

		// Набор сформированных оповещений
		private NotificationsSet notifications;

		// Шаг частоты обновления оповещений (используется для корректного отображения в интерфейсе)
		private uint updatingFrequencyStep;

		// Дескрипторы диалоговых окон
		private OpenFileDialog ofd;
		private SaveFileDialog sfd;

		// Контекстное меню списка оповещений
		private ContextMenuStrip notMenu;

		// Число оповещений, видимых в поле списка без прокрутки
		private const uint visibleNotificationsInList = 8;

		// Индекс оповещения, от которого был направлен запрос на отображение контекстного меню
		private int notSender = -1;

		// Флаг, сообщающий основному интерфейсу о необходимости полного повторного опроса всех оповещений
		private bool completeUpdate = false;

		/// <summary>
		/// Возвращает флаг полного опроса оповещений
		/// </summary>
		public bool CompleteUpdate
			{
			get
				{
				return completeUpdate;
				}
			}

		/// <summary>
		/// Конструктор. Настраивает главную форму приложения
		/// </summary>
		/// <param name="Notifications">Набор загруженных оповещений</param>
		/// <param name="UpdatingFrequencyStep">Шаг изменения частоты обновления</param>
		/// <param name="NotificationForSetup">Номер оповещения для настройки. Выбирается в
		/// списке при старте интерфейса. При отрицательном значении параметр игнорируется</param>
		public UNGenSettingsForm (NotificationsSet Notifications, uint UpdatingFrequencyStep,
			int NotificationForSetup)
			{
			// Инициализация
			InitializeComponent ();
			notifications = Notifications;
			updatingFrequencyStep = UpdatingFrequencyStep;

			// Настройка контролов
			this.Text = ProgramDescription.AssemblyVisibleName;
			this.CancelButton = BClose;

			RDGenerics.LoadWindowDimensions (this);

			LanguageCombo.Items.AddRange (RDLocale.LanguagesNames);

			ofd = new OpenFileDialog ();
			sfd = new SaveFileDialog ();
			ofd.Title = sfd.Title = ProgramDescription.AssemblyVisibleName;
			ofd.CheckFileExists = ofd.CheckPathExists = true;
			sfd.OverwritePrompt = true;
			ofd.Multiselect = false;
			ofd.RestoreDirectory = sfd.RestoreDirectory = true;
			ofd.ShowHelp = ofd.ShowReadOnly = sfd.ShowHelp = false;

			// Загрузка параметров
			WindowCallFlag.Checked = NotificationsSupport.CallWindowOnUrgents;
			UrgentSigField.Text = Notification.UrgentSignatures;

			TranslucencyField.Checked = NotificationsSupport.TranslucentLogItems;
			FontSizeField.Value = NotificationsSupport.LogFontSize / 10.0m;

			try
				{
				LanguageCombo.SelectedIndex = (int)RDLocale.CurrentLanguage;
				}
			catch
				{
				LanguageCombo.SelectedIndex = 0;
				}

			// Загрузка оповещений в список
			LoadNotifications ();

			if (NotificationForSetup >= 0)
				notSender = NotificationForSetup;

			// Запуск
			ProgramDescription.ShowTip (NSTipTypes.StartupTip);
			this.ShowDialog ();
			}

		private void UNGenSettingsForm_Shown (object sender, EventArgs e)
			{
			// Был передан номер уведомления для настройки
			if (notSender >= 0)
				NotMenuSetup_Click (null, null);
			}

		// Метод (пере)загружает уведомления в список
		private void LoadNotifications ()
			{
			NotLayout.Controls.Clear ();
			BWizard.Enabled = (notifications.Notifications.Count < NotificationsSet.MaxNotifications);

			for (int i = 0; i < notifications.Notifications.Count; i++)
				{
				// Формирование контрола
				Label l = new Label ();
				l.AutoSize = false;

				if (notifications.Notifications[i].IsEnabled)
					l.BackColor = RDInterface.GetInterfaceColor (RDInterfaceColors.WarningMessage);
				else
					l.BackColor = RDInterface.GetInterfaceColor (RDInterfaceColors.MediumGrey);
				l.ForeColor = RDInterface.GetInterfaceColor (RDInterfaceColors.DefaultText);

				l.Click += NotLabel_Clicked;
				l.Font = this.Font;
				l.Text = notifications.Notifications[i].Name;
				l.Margin = new Padding (3, 3, 3, 3);

				l.MaximumSize = l.MinimumSize = new Size (NotLayout.Width - 6 -
					((notifications.Notifications.Count > visibleNotificationsInList) ? 18 : 0), 0);
				l.AutoSize = true;

				// Добавление
				NotLayout.Controls.Add (l);
				}
			}

		// Метод вызывает контекстное меню при нажатии на уведомление
		private void NotLabel_Clicked (object sender, EventArgs e)
			{
			// Определение вызывающего элемента
			Label l = (Label)sender;
			notSender = NotLayout.Controls.IndexOf (l);

			// Настройка меню
			notMenu.Items[3].Enabled = (notifications.Notifications.Count > 1);
			notMenu.Items[1].Enabled = (notifications.Notifications.Count < NotificationsSet.MaxNotifications);

			// Отображение
			notMenu.Show (l, Point.Empty);
			}

		// Закрытие окна просмотра
		private void BClose_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		private void BCloseAndRequest_Click (object sender, EventArgs e)
			{
			completeUpdate = true;
			this.Close ();
			}

		private void UNGenSettingsForm_FormClosing (object sender, FormClosingEventArgs e)
			{
			// Сохранение настроек
			notifications.SaveNotifications ();
			RDGenerics.SaveWindowDimensions (this);

			// Закрытие окна
			ProgramDescription.ShowTip (NSTipTypes.ServiceLaunchTip);
			}

		// Добавление оповещения копированием
		private void NotMenuAddCopy_Click (object sender, EventArgs e)
			{
			// Инициализация экземпляра
			Notification n = notifications.Notifications[notSender].CloneNotification ();

			// Коррекция названия
			n = MakeUniqueName (n, -2);

			// Обновление и запуск на редактирование
			notifications.Notifications.Add (n);
			RDInterface.MessageBox (RDMessageFlags.Success | RDMessageFlags.CenterText,
				RDLocale.GetText ("NotAddedMessage") + n.Name, 750);

			LoadNotifications ();   // Последующие изменения могут быть отменены

			notSender = notifications.Notifications.Count - 1;
			NotMenuSetup_Click (null, null);
			}

		// Настройка оповещения
		private void NotMenuSetup_Click (object sender, EventArgs e)
			{
			// Редактирование
			UNNotSettingsForm unnsf = new UNNotSettingsForm (notifications.Notifications[notSender],
				updatingFrequencyStep);
			if (!unnsf.ChangesApplied)
				{
				unnsf.Dispose ();
				return;
				}

			// Контроль позиции
			Notification n = unnsf.NewNotificationItem;
			unnsf.Dispose ();
			n = MakeUniqueName (n, notSender);

			// Обновление
			notifications.Notifications[notSender] = n;
			RDInterface.MessageBox (RDMessageFlags.Success | RDMessageFlags.CenterText,
				RDLocale.GetText ("NotUpdatedMessage") + n.Name, 750);

			LoadNotifications ();
			NotLayout.ScrollControlIntoView (NotLayout.Controls[notSender]);
			}

		// Удаление оповещения
		private void NotMenuDelete_Click (object sender, EventArgs e)
			{
			// Контроль
			if (RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
				"DeleteMessage", RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) ==
				RDMessageButtons.ButtonTwo)
				return;

			// Удаление
			notifications.Notifications.RemoveAt (notSender);
			RDInterface.LocalizedMessageBox (RDMessageFlags.Success | RDMessageFlags.CenterText,
				"NotRemovedMessage", 750);

			// Обновление
			LoadNotifications ();
			}

		// Локализация формы
		private void LanguageCombo_SelectedIndexChanged (object sender, EventArgs e)
			{
			// Сохранение
			RDLocale.CurrentLanguage = (RDLanguages)LanguageCombo.SelectedIndex;

			// Локализация
			RDLocale.SetControlsText (this);
			BClose.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Close);
			LanguageLabel.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Control_InterfaceLanguage);
			BWizard.Text = RDLocale.GetText ("NotMenu_AddWizard");
			BCloseAndRequest.Text = RDLocale.GetText ("BCloseAndRequest");

			ofd.Filter = sfd.Filter = RDLocale.GetText (NotificationsSet.SettingsFileExtension + "file") + "|*." +
				NotificationsSet.SettingsFileExtension;

			LogColorField.Items.Clear ();
			LogColorField.Items.AddRange (NotificationsSupport.LogColors.ColorNames);
			LogColorField.SelectedIndex = (int)NotificationsSupport.LogColor;

			// Формирование контекстного меню
			if (notMenu == null)
				{
				notMenu = new ContextMenuStrip ();
				notMenu.ShowImageMargin = false;
				}
			notMenu.Items.Clear ();

			notMenu.Items.Add (RDLocale.GetText ("NotMenu_Setup"), null, NotMenuSetup_Click);
			notMenu.Items.Add (RDLocale.GetText ("NotMenu_AddCopy"), null, NotMenuAddCopy_Click);
			notMenu.Items.Add (RDLocale.GetText ("NotMenu_Share"), null, NotMenuShare_Click);
			notMenu.Items.Add (RDLocale.GetDefaultText (RDLDefaultTexts.Button_Delete), null, NotMenuDelete_Click);
			}

		// Выгрузка настроек в буфер обмена
		private void NotMenuShare_Click (object sender, EventArgs e)
			{
			// Подсказка
			ProgramDescription.ShowTip (NSTipTypes.ShareSettings);

			// Выбор варианта выгрузки
			switch (RDInterface.MessageBox (RDMessageFlags.Question,
				RDLocale.GetText ("ShareVariant"),
				RDLocale.GetText ("ShareFile"), RDLocale.GetText ("ShareClipboard"),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel)))
				{
				// Сохранение в файл
				case RDMessageButtons.ButtonOne:
					// Запрос пути
					sfd.FileName = NotificationsSet.SettingsFileName;
					if (sfd.ShowDialog () != DialogResult.OK)
						return;

					// Сохранение
					try
						{
						File.WriteAllText (sfd.FileName, notifications.GetSettingsList (),
							RDGenerics.GetEncoding (RDEncodings.Unicode16));
						}
					catch
						{
						RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
							string.Format (RDLocale.GetDefaultText (RDLDefaultTexts.Message_SaveFailure_Fmt),
							sfd.FileName));
						}
					break;

				// Копирование
				case RDMessageButtons.ButtonTwo:
					string spl = NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString ();
					string sett = notifications.Notifications[notSender].Name + spl;
					sett += notifications.Notifications[notSender].Link + spl;
					sett += notifications.Notifications[notSender].Beginning + spl;
					sett += notifications.Notifications[notSender].Ending + spl;
					sett += notifications.Notifications[notSender].OccurrenceNumber.ToString ();

					RDGenerics.SendToClipboard (sett, true);
					break;
				}
			}

		// Вызов мастера оповещений
		private void NotMenuAddWizard_Click (object sender, EventArgs e)
			{
			// Запрос
			WizardForm wf = new WizardForm (notifications, updatingFrequencyStep);

			// Обновление
			if (wf.Cancelled)
				{
				wf.Dispose ();
				return;
				}

			// Обработка случая с файлом
			if (wf.CreateFromFile)
				{
				// Запрос файла
				if (ofd.ShowDialog () != DialogResult.OK)
					return;

				if (RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"LoadingWarning", RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) !=
					RDMessageButtons.ButtonOne)
					return;

				string settings;
				try
					{
					settings = File.ReadAllText (ofd.FileName,
						RDGenerics.GetEncoding (RDEncodings.Unicode16));
					}
				catch
					{
					RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
						string.Format (RDLocale.GetDefaultText (RDLDefaultTexts.Message_LoadFailure_Fmt),
						ofd.FileName));
					return;
					}

				// Загрузка оповещений
				notifications.SetSettingsList (settings);
				LoadNotifications ();

				wf.Dispose ();
				return;
				}

			// Обработка возврата мастера
			NotConfiguration cfg;
			cfg.NotificationName = wf.NotificationName;
			cfg.SourceLink = wf.NotificationLink;
			cfg.WatchAreaBeginningSign = wf.NotificationBeginning;
			cfg.WatchAreaEndingSign = wf.NotificationEnding;
			cfg.UpdatingFrequency = wf.UpdateFrequenciesListIndex + 1;
			cfg.OccurrenceNumber = wf.NotificationOccurrence;

			cfg.ComparisonType = NotComparatorTypes.Disabled;
			cfg.ComparisonString = "";
			cfg.IgnoreComparisonMisfits = false;
			cfg.NotifyWhenUnavailable = false;

			wf.Dispose ();

			// Добавление и обновление
			// Инициализация экземпляра
			Notification n = new Notification (cfg);

			// Коррекция названия
			n = MakeUniqueName (n, -1);

			// Добавление и обнволение
			notifications.Notifications.Add (n);
			RDInterface.MessageBox (RDMessageFlags.Success | RDMessageFlags.CenterText,
				RDLocale.GetText ("NotAddedMessage") + n.Name, 750);

			LoadNotifications ();
			NotLayout.ScrollControlIntoView (NotLayout.Controls[NotLayout.Controls.Count - 1]);
			}

		private Notification MakeUniqueName (Notification Not, int ItemForUpdate)
			{
			bool nameAdjusted = false;
			int idx = notifications.Notifications.IndexOf (Not);
			while ((idx >= 0) && (idx != ItemForUpdate))
				{
				Not.MakeUniqueName ();
				idx = notifications.Notifications.IndexOf (Not);

				nameAdjusted = true;
				}

			if (nameAdjusted && (ItemForUpdate > -2))
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"NotMatchingNames");

			return Not;
			}

		// Переключение флага вызова окна журнала при срочных оповещениях
		private void WindowCallFlag_CheckedChanged (object sender, EventArgs e)
			{
			NotificationsSupport.CallWindowOnUrgents = WindowCallFlag.Checked;
			}

		// Выбор цветовой схемы журнала
		private void LogColorField_SelectedIndexChanged (object sender, EventArgs e)
			{
			NotificationsSupport.LogColor = (uint)LogColorField.SelectedIndex;
			}

		// Выбор полупрозрачности для элементов журнала
		private void TranslucencyField_CheckedChanged (object sender, EventArgs e)
			{
			NotificationsSupport.TranslucentLogItems = TranslucencyField.Checked;
			}

		// Изменение размера шрифта
		private void FontSizeField_ValueChanged (object sender, EventArgs e)
			{
			NotificationsSupport.LogFontSize = (uint)(FontSizeField.Value * 10.0m);
			}

		// Изменение набора признаков срочности
		private void UrgentSigField_TextChanged (object sender, EventArgs e)
			{
			Notification.UrgentSignatures = UrgentSigField.Text;
			}
		}
	}
