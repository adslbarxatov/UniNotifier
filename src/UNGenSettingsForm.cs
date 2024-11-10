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

		// Стандартная задержка самозакрывающихся сообщений
		private const uint messagesTimeout = 1000;

		// Контекстное меню списка оповещений
		private ContextMenu notMenu;

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

			/*for (uint i = 1; i <= 24; i++)
				FrequencyCombo.Items.Add ((i * UpdatingFrequencyStep).ToString ());
			FrequencyCombo.SelectedIndex = 2;
			EnabledCheck.Checked = true;

			OccurrenceField.Minimum = 1;
			OccurrenceField.Maximum = Notification.MaxOccurrenceNumber;

			NameText.MaxLength = BeginningText.MaxLength = EndingText.MaxLength = Notification.MaxBeginningEndingLength;

			ComparatorValue.MouseWheel += ComparatorValue_MouseWheel;*/

			// Загрузка оповещений в список
			LoadNotifications ();

			/*UpdateButtons ();

			for (int i = 0; i < notifications.Notifications.Count; i++)
				NotificationsList.Items.Add (notifications.Notifications[i].Name +
					(notifications.Notifications[i].IsEnabled ? " (+)" : " (–)"));
			if (NotificationsList.Items.Count > 0)
				{
				if (NotificationForSetup < 0)
					NotificationsList.SelectedIndex = 0;
				else
					NotificationsList.SelectedIndex = NotificationForSetup;
				}*/
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

			for (int i = 0; i < notifications.Notifications.Count; i++)
				{
				// Формирование контрола
				Label l = new Label ();
				l.AutoSize = false;

				if (notifications.Notifications[i].IsEnabled)
					l.BackColor = RDGenerics.GetInterfaceColor (RDInterfaceColors.WarningMessage);
				else
					l.BackColor = RDGenerics.GetInterfaceColor (RDInterfaceColors.MediumGrey);
				l.ForeColor = RDGenerics.GetInterfaceColor (RDInterfaceColors.DefaultText);

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
			notMenu.MenuItems[3].Enabled = (notifications.Notifications.Count > 1);
			notMenu.MenuItems[1].Enabled = notMenu.MenuItems[5].Enabled =
				(notifications.Notifications.Count < NotificationsSet.MaxNotifications);

			// Отображение
			notMenu.Show (l, Point.Empty);
			}

		/*// Обновление состояния кнопок
		private void UpdateButtons ()
			{
			BAdd.Enabled = NotWizard.Enabled = (notifications.Notifications.Count < NotificationsSet.MaxNotifications);
			BDelete.Enabled = (notifications.Notifications.Count > 1);    // Одно должно остаться
			}*/

		// Закрытие окна просмотра
		private void BClose_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		private void UNGenSettingsForm_FormClosing (object sender, FormClosingEventArgs e)
			{
			// Сохранение настроек
			notifications.SaveNotifications ();
			RDGenerics.SaveWindowDimensions (this);

			// Закрытие окна
			ProgramDescription.ShowTip (NSTipTypes.ServiceLaunchTip);

			completeUpdate = RDGenerics.LocalizedMessageBox (RDMessageTypes.Question_Center, "RecallAllNews",
				RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) ==
				RDMessageButtons.ButtonOne;
			}

		/*// Загрузка значений в поля
		private void NotificationsList_Select (object sender, EventArgs e)
			{
			// Контроль
			if (NotificationsList.SelectedIndex < 0)
				return;
			else if (NotificationsList.SelectedIndex != 0)
				ProgramDescription.ShowTip (NSTipTypes.CurrentNotButton);

			// Загрузка
			int i = NotificationsList.SelectedIndex;
			NameText.Text = notifications.Notifications[i].Name;
			LinkText.Text = notifications.Notifications[i].Link;
			BeginningText.Text = notifications.Notifications[i].Beginning;
			EndingText.Text = notifications.Notifications[i].Ending;
			FrequencyCombo.SelectedIndex = (int)notifications.Notifications[i].UpdateFrequency - 1;
			EnabledCheck.Checked = notifications.Notifications[i].IsEnabled;
			OccurrenceField.Value = notifications.Notifications[i].OccurrenceNumber;

			ComparatorFlag.Checked = (notifications.Notifications[i].ComparisonType != NotComparatorTypes.Disabled);
			ComparatorValue.Text = notifications.Notifications[i].ComparisonString;
			MisfitsFlag.Checked = notifications.Notifications[i].IgnoreComparisonMisfits;
			CheckAvailability.Checked = notifications.Notifications[i].NotifyIfSourceIsUnavailable;

			if (ComparatorFlag.Checked)
				ComparatorType.SelectedIndex = (int)notifications.Notifications[i].ComparisonType;
			}*/

		// Добавление оповещения копированием
		private void NotMenuAddCopy_Click (object sender, EventArgs e)
			{
			/*// Добавление
			ProgramDescription.ShowTip (NSTipTypes.AddButton);
			UpdateItem (-1);

			// Обновление кнопок
			UpdateButtons ();*/

			// Инициализация экземпляра
			Notification n = notifications.Notifications[notSender].CloneNotification ();

			// Коррекция названия
			n = MakeUniqueName (n, -2);
			/*bool nameAdjusted = false;
			while (notifications.Notifications.IndexOf (n) >= 0)
				{
				n.MakeUniqueName ();
				nameAdjusted = true;
				}

			if (nameAdjusted)
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning_Center, "NotMatchingNames");*/

			// Обновление и запуск на редактирование
			notifications.Notifications.Add (n);
			RDGenerics.MessageBox (RDMessageTypes.Success_Center,
				RDLocale.GetText ("NotAddedMessage") + n.Name, messagesTimeout);

			LoadNotifications ();   // Последующие изменения могут быть отменены

			notSender = notifications.Notifications.Count - 1;
			NotMenuSetup_Click (null, null);
			/*LoadNotifications ();
			NotLayout.ScrollControlIntoView (NotLayout.Controls[NotLayout.Controls.Count - 1]);*/
			}

		// Настройка оповещения
		private void NotMenuSetup_Click (object sender, EventArgs e)
			{
			/*ProgramDescription.ShowTip (NSTipTypes.ApplyButton);
			UpdateItem (NotificationsList.SelectedIndex);*/
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

			/*bool nameAdjusted = false;
			while (notifications.Notifications.IndexOf (n) >= 0)
				{
				n.MakeUniqueName ();
				nameAdjusted = true;
				}

			if (nameAdjusted)
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning_Center, "NotMatchingNames");*/

			// Обновление
			notifications.Notifications[notSender] = n;
			RDGenerics.MessageBox (RDMessageTypes.Success_Center,
				RDLocale.GetText ("NotUpdatedMessage") + n.Name, messagesTimeout);

			LoadNotifications ();
			NotLayout.ScrollControlIntoView (NotLayout.Controls[notSender]);
			}

		/*// Метод обновления оповещений (номер -1 – добавление нового)
		private void UpdateItem (int ItemNumber)
			{
			// Инициализация оповещения
			NotConfiguration cfg;
			cfg.NotificationName = NameText.Text;
			cfg.SourceLink = LinkText.Text;
			cfg.WatchAreaBeginningSign = BeginningText.Text;
			cfg.WatchAreaEndingSign = EndingText.Text;
			cfg.UpdatingFrequency = (uint)(FrequencyCombo.SelectedIndex + 1);
			cfg.OccurrenceNumber = (uint)OccurrenceField.Value;
			cfg.ComparisonType = ComparatorFlag.Checked ? (NotComparatorTypes)ComparatorType.SelectedIndex :
				NotComparatorTypes.Disabled;
			cfg.ComparisonString = ComparatorValue.Text;
			cfg.IgnoreComparisonMisfits = MisfitsFlag.Checked;
			cfg.NotifyWhenUnavailable = CheckAvailability.Checked;

			Notification ni = new Notification (cfg);

			if (!ni.IsInited)
				{
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning_Center, "NotEnoughDataMessage");
				return;
				}

			// Условие не выполняется только в двух случаях:
			// - когда добавляется новое оповещение, не имеющее аналогов в списке;
			// - когда обновляется текущее выбранное оповещение.
			// Остальные случаи следует считать попыткой задвоения имени
			int idx = notifications.Notifications.IndexOf (ni);
			if ((idx >= 0) && (idx != ItemNumber))
				{
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning_Center, "NotMatchingNames");
				return;
				}

			ni.IsEnabled = EnabledCheck.Checked;

			// Добавление
			if (ItemNumber < 0)
				{
				notifications.Notifications.Add (ni);
				NotificationsList.Items.Add (ni.Name + (ni.IsEnabled ? " (+)" : " (–)"));
				}
			else if (ItemNumber < NotificationsList.Items.Count)
				{
				notifications.Notifications[ItemNumber] = ni;
				NotificationsList.Items[ItemNumber] = ni.Name + (ni.IsEnabled ? " (+)" : " (–)");
				}
			else
				{
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning_Center, "UpdateLineNotSpecified");
				return;
				}

			// Переключение на новую позицию в случае добавления
			if (ItemNumber < 0)
				NotificationsList.SelectedIndex = NotificationsList.Items.Count - 1;

			RDGenerics.MessageBox (RDMessageTypes.Success_Center,
				RDLocale.GetText (ItemNumber < 0 ? "NotAddedMessage" : "NotUpdatedMessage") + ni.Name,
				messagesTimeout);
			}*/

		// Удаление оповещения
		private void NotMenuDelete_Click (object sender, EventArgs e)
			{
			// Контроль
			/*ProgramDescription.ShowTip (NSTipTypes.DeleteButton);
			if (NotificationsList.SelectedIndex < 0)
				{
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning_Center, "DeleteLineNotSpecified");
				return;
				}*/

			if (RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning_Center, "DeleteMessage",
				RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) ==
				RDMessageButtons.ButtonTwo)
				return;

			// Удаление
			/*int index = NotificationsList.SelectedIndex;
			NotificationsList.Items.RemoveAt (index);*/
			notifications.Notifications.RemoveAt (notSender);
			RDGenerics.LocalizedMessageBox (RDMessageTypes.Success_Center, "NotRemovedMessage", messagesTimeout);

			// Обновление
			LoadNotifications ();

			/*// Переключение
			if (NotificationsList.Items.Count > 0)
				NotificationsList.SelectedIndex = (index >= NotificationsList.Items.Count) ?
					(NotificationsList.Items.Count - 1) : index;

			// Обновление кнопок
			UpdateButtons ();*/
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

			ofd.Filter = sfd.Filter = RDLocale.GetText (NotificationsSet.SettingsFileExtension + "file") + "|*." +
				NotificationsSet.SettingsFileExtension;

			LogColorField.Items.Clear ();
			LogColorField.Items.AddRange (NotificationsSupport.LogColors.ColorNames);
			LogColorField.SelectedIndex = (int)NotificationsSupport.LogColor;

			// Формирование контекстного меню
			if (notMenu == null)
				notMenu = new ContextMenu ();
			notMenu.MenuItems.Clear ();

			notMenu.MenuItems.Add (new MenuItem (RDLocale.GetText ("NotMenu_Setup"), NotMenuSetup_Click));
			notMenu.MenuItems.Add (new MenuItem (RDLocale.GetText ("NotMenu_AddCopy"), NotMenuAddCopy_Click));
			notMenu.MenuItems.Add (new MenuItem (RDLocale.GetText ("NotMenu_Share"), NotMenuShare_Click));
			notMenu.MenuItems.Add (new MenuItem (RDLocale.GetDefaultText (RDLDefaultTexts.Button_Delete),
				NotMenuDelete_Click));
			notMenu.MenuItems.Add (new MenuItem ("-"));
			notMenu.MenuItems.Add (new MenuItem (RDLocale.GetText ("NotMenu_AddWizard"), NotMenuAddWizard_Click));

			/*BDelete.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Delete);
			BUpdate.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Update);

			int idx = ComparatorType.SelectedIndex;
			char[] ctSplitter = new char[] { '\n' };

			ComparatorType.Items.Clear ();
			ComparatorType.Items.AddRange (RDLocale.GetText ("ComparatorTypes").Split (ctSplitter));

			if (idx >= 0)
				ComparatorType.SelectedIndex = idx;
			else
				ComparatorType.SelectedIndex = 0;*/
			}

		/*// Подсказка по полю Occurence
		private void OccurrenceField_Click (object sender, EventArgs e)
			{
			ProgramDescription.ShowTip (NSTipTypes.OccurenceTip);
			}*/

		// Выгрузка настроек в буфер обмена
		private void NotMenuShare_Click (object sender, EventArgs e)
			{
			// Подсказка
			ProgramDescription.ShowTip (NSTipTypes.ShareSettings);

			// Выбор варианта выгрузки
			switch (RDGenerics.MessageBox (RDMessageTypes.Question_Left, RDLocale.GetText ("ShareVariant"),
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
						RDGenerics.MessageBox (RDMessageTypes.Warning_Center,
							string.Format (RDLocale.GetDefaultText (RDLDefaultTexts.Message_SaveFailure_Fmt),
							sfd.FileName));
						}
					break;

				// Копирование
				case RDMessageButtons.ButtonTwo:
					/*try
						{
						Clipboard.SetText (NameText.Text +
							NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () +
							LinkText.Text + NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () +
							BeginningText.Text +
							NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () +
							EndingText.Text + NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () +
							((uint)(OccurrenceField.Value)).ToString ());
						}
					catch { }*/
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
				/*ofd.FileName = NotificationsSet.SettingsFileName;*/
				if (ofd.ShowDialog () != DialogResult.OK)
					return;

				if (RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning_Center, "LoadingWarning",
					RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) !=
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
					RDGenerics.MessageBox (RDMessageTypes.Warning_Center,
						string.Format (RDLocale.GetDefaultText (RDLDefaultTexts.Message_LoadFailure_Fmt),
						ofd.FileName));
					return;
					}

				// Загрузка оповещений
				notifications.SetSettingsList (settings);
				LoadNotifications ();

				/*// Загрузка оповещений в список
				UpdateButtons ();

				NotificationsList.Items.Clear ();
				for (int i = 0; i < notifications.Notifications.Count; i++)
					NotificationsList.Items.Add (notifications.Notifications[i].Name +
						(notifications.Notifications[i].IsEnabled ? " (+)" : " (–)"));
				if (NotificationsList.Items.Count > 0)
					NotificationsList.SelectedIndex = 0;*/

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
			/*EnabledCheck.Checked = true;*/

			cfg.ComparisonType = NotComparatorTypes.Disabled;
			cfg.ComparisonString = "";
			cfg.IgnoreComparisonMisfits = false;
			cfg.NotifyWhenUnavailable = false;

			wf.Dispose ();
			/*UpdateItem (-1);
			UpdateButtons ();*/

			// Добавление и обновление
			// Инициализация экземпляра
			Notification n = new Notification (cfg);

			// Коррекция названия
			n = MakeUniqueName (n, -1);
			/*bool nameAdjusted = false;
			while (notifications.Notifications.IndexOf (n) >= 0)
				{
				n.MakeUniqueName ();
				nameAdjusted = true;
				}

			if (nameAdjusted)
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning_Center, "NotMatchingNames");*/

			// Добавление и обнволение
			notifications.Notifications.Add (n);
			RDGenerics.MessageBox (RDMessageTypes.Success_Center,
				RDLocale.GetText ("NotAddedMessage") + n.Name, messagesTimeout);

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
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning_Center, "NotMatchingNames");

			return Not;
			}

		/*// Изменение состояния функции
		private void ComparatorFlag_CheckedChanged (object sender, EventArgs e)
			{
			if (ComparatorFlag.Checked)
				ProgramDescription.ShowTip (NSTipTypes.Threshold);

			ComparatorType.Enabled = ComparatorValue.Enabled = MisfitsFlag.Enabled = ComparatorFlag.Checked;
			}*/

		/*// Изменение значения компаратора
		private void ComparatorValue_KeyDown (object sender, KeyEventArgs e)
			{
			switch (e.KeyCode)
				{
				case Keys.Up:
				case Keys.Down:
					UpdateComparatorValue (e.KeyCode == Keys.Up);
					break;
				}
			}*/

		/*private void ComparatorValue_MouseWheel (object sender, MouseEventArgs e)
			{
			if (e.Delta > 0)
				UpdateComparatorValue (true);
			else if (e.Delta < 0)
				UpdateComparatorValue (false);
			}*/

		/*private void UpdateComparatorValue (bool Increase)
			{
			double v = 0.0;
			try
				{
				v = double.Parse (ComparatorValue.Text.Replace (',', '.'),
					RDLocale.GetCulture (RDLanguages.en_us));
				}
			catch { }

			if (Increase)
				v += 1.0;
			else
				v -= 1.0;

			ComparatorValue.Text = v.ToString (RDLocale.GetCulture (RDLanguages.en_us));
			}*/

		/*// Изменение состояния «включено»
		private void EnabledCheck_CheckedChanged (object sender, EventArgs e)
			{
			EnabledCheck.Text = (EnabledCheck.Checked ? "4" : ";");
			}*/

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
