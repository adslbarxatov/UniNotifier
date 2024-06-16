using System;
using System.IO;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает форму настроек оповещений
	/// </summary>
	public partial class SettingsForm: Form
		{
		// Переменные и константы
		private NotificationsSet notifications;
		private uint updatingFrequencyStep;
		private OpenFileDialog ofd;
		private SaveFileDialog sfd;
		private const uint messagesTimeout = 1000;

		/// <summary>
		/// Конструктор. Настраивает главную форму приложения
		/// </summary>
		/// <param name="Notifications">Набор загруженных оповещений</param>
		/// <param name="UpdatingFrequencyStep">Шаг изменения частоты обновления</param>
		/// <param name="CallWindowOnUrgents">Флаг вызова главного окна при срочных оповещениях</param>
		public SettingsForm (NotificationsSet Notifications, uint UpdatingFrequencyStep, bool CallWindowOnUrgents)
			{
			// Инициализация
			InitializeComponent ();
			notifications = Notifications;
			updatingFrequencyStep = UpdatingFrequencyStep;
			WindowCallFlag.Checked = CallWindowOnUrgents;

			this.Text = ProgramDescription.AssemblyVisibleName;
			this.CancelButton = BClose;

			RDGenerics.LoadWindowDimensions (this);

			ofd = new OpenFileDialog ();
			sfd = new SaveFileDialog ();
			ofd.Filter = sfd.Filter = RDLocale.GetText (NotificationsSet.SettingsFileExtension + "file") + "|" +
				NotificationsSet.SettingsFileName;
			ofd.Title = sfd.Title = ProgramDescription.AssemblyVisibleName;
			ofd.CheckFileExists = ofd.CheckPathExists = true;
			sfd.OverwritePrompt = true;
			ofd.Multiselect = false;
			ofd.RestoreDirectory = sfd.RestoreDirectory = true;
			ofd.ShowHelp = ofd.ShowReadOnly = sfd.ShowHelp = false;

			LanguageCombo.Items.AddRange (RDLocale.LanguagesNames);
			try
				{
				LanguageCombo.SelectedIndex = (int)RDLocale.CurrentLanguage;
				}
			catch
				{
				LanguageCombo.SelectedIndex = 0;
				}

			for (uint i = 1; i <= 24; i++)
				FrequencyCombo.Items.Add ((i * UpdatingFrequencyStep).ToString ());
			FrequencyCombo.SelectedIndex = 2;
			EnabledCheck.Checked = true;

			OccurrenceField.Minimum = 1;
			OccurrenceField.Maximum = Notification.MaxOccurrenceNumber;

			NameText.MaxLength = BeginningText.MaxLength = EndingText.MaxLength = Notification.MaxBeginningEndingLength;

			ComparatorValue.MouseWheel += ComparatorValue_MouseWheel;

			// Загрузка оповещений в список
			UpdateButtons ();

			for (int i = 0; i < notifications.Notifications.Count; i++)
				NotificationsList.Items.Add (notifications.Notifications[i].Name +
					(notifications.Notifications[i].IsEnabled ? " (+)" : " (–)"));
			if (NotificationsList.Items.Count > 0)
				NotificationsList.SelectedIndex = 0;

			// Запуск
			ProgramDescription.ShowTip (NSTipTypes.StartupTip);
			this.ShowDialog ();
			}

		// Обновление состояния кнопок
		private void UpdateButtons ()
			{
			BAdd.Enabled = NotWizard.Enabled = (notifications.Notifications.Count < NotificationsSet.MaxNotifications);
			BDelete.Enabled = (notifications.Notifications.Count > 1);    // Одно должно остаться
			}

		/// <summary>
		/// Возвращает флаг вызова главного окна при срочных оповещениях
		/// </summary>
		public bool CallWindowOnUrgents
			{
			get
				{
				return callWindowOnUrgents;
				}
			}
		private bool callWindowOnUrgents = false;

		// Закрытие окна просмотра
		private void BClose_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		private void SettingsForm_FormClosing (object sender, FormClosingEventArgs e)
			{
			// Сохранение настроек
			notifications.SaveNotifications ();
			callWindowOnUrgents = WindowCallFlag.Checked;

			RDGenerics.SaveWindowDimensions (this);

			// Закрытие окна
			ProgramDescription.ShowTip (NSTipTypes.ServiceLaunchTip);

			completeUpdate = RDGenerics.LocalizedMessageBox (RDMessageTypes.Question_Center, "RecallAllNews",
				RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) ==
				RDMessageButtons.ButtonOne;
			}

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
		private bool completeUpdate = false;

		// Загрузка значений в поля
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
			}

		// Добавление и обновление позиций
		private void BAdd_Click (object sender, EventArgs e)
			{
			// Добавление
			ProgramDescription.ShowTip (NSTipTypes.AddButton);
			UpdateItem (-1);

			// Обновление кнопок
			UpdateButtons ();
			}

		private void BUpdate_Click (object sender, EventArgs e)
			{
			ProgramDescription.ShowTip (NSTipTypes.ApplyButton);
			UpdateItem (NotificationsList.SelectedIndex);
			}

		// Метод обновления оповещений (номер -1 – добавление нового)
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
			}

		// Удаление оповещения
		private void BDelete_Click (object sender, EventArgs e)
			{
			// Контроль
			ProgramDescription.ShowTip (NSTipTypes.DeleteButton);
			if (NotificationsList.SelectedIndex < 0)
				{
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning_Center, "DeleteLineNotSpecified");
				return;
				}

			if (RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning_Center, "DeleteMessage",
				RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) ==
				RDMessageButtons.ButtonTwo)
				return;

			// Удаление
			int index = NotificationsList.SelectedIndex;
			notifications.Notifications.RemoveAt (index);
			NotificationsList.Items.RemoveAt (index);
			RDGenerics.LocalizedMessageBox (RDMessageTypes.Success_Center, "NotRemovedMessage", messagesTimeout);

			// Переключение
			if (NotificationsList.Items.Count > 0)
				NotificationsList.SelectedIndex = (index >= NotificationsList.Items.Count) ?
					(NotificationsList.Items.Count - 1) : index;

			// Обновление кнопок
			UpdateButtons ();
			}

		// Локализация формы
		private void LanguageCombo_SelectedIndexChanged (object sender, EventArgs e)
			{
			// Сохранение
			RDLocale.CurrentLanguage = (RDLanguages)LanguageCombo.SelectedIndex;

			// Локализация
			RDLocale.SetControlsText (this);
			BDelete.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Delete);
			BUpdate.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Update);

			int idx = ComparatorType.SelectedIndex;
			char[] ctSplitter = new char[] { '\n' };

			ComparatorType.Items.Clear ();
			ComparatorType.Items.AddRange (RDLocale.GetText ("ComparatorTypes").Split (ctSplitter));

			if (idx >= 0)
				ComparatorType.SelectedIndex = idx;
			else
				ComparatorType.SelectedIndex = 0;
			}

		// Подсказка по полю Occurence
		private void OccurrenceField_Click (object sender, EventArgs e)
			{
			ProgramDescription.ShowTip (NSTipTypes.OccurenceTip);
			}

		// Выгрузка настроек в буфер обмена
		private void ShareSettings_Click (object sender, EventArgs e)
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
					try
						{
						Clipboard.SetText (NameText.Text +
							NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () +
							LinkText.Text + NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () +
							BeginningText.Text +
							NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () +
							EndingText.Text + NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () +
							((uint)(OccurrenceField.Value)).ToString ());
						}
					catch { }
					break;
				}
			}

		// Вызов мастера оповещений
		private void NotWizard_Click (object sender, EventArgs e)
			{
			// Запрос
			WizardForm wf = new WizardForm (notifications, updatingFrequencyStep, (uint)FrequencyCombo.Items.Count);

			// Обновление
			if (wf.Cancelled)
				return;

			// Обработка случая с файлом
			if (wf.CreateFromFile)
				{
				// Запрос файла
				ofd.FileName = NotificationsSet.SettingsFileName;
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
				notifications.SetSettingsList (settings);

				// Загрузка оповещений в список
				UpdateButtons ();

				NotificationsList.Items.Clear ();
				for (int i = 0; i < notifications.Notifications.Count; i++)
					NotificationsList.Items.Add (notifications.Notifications[i].Name +
						(notifications.Notifications[i].IsEnabled ? " (+)" : " (–)"));
				if (NotificationsList.Items.Count > 0)
					NotificationsList.SelectedIndex = 0;
				return;
				}

			NameText.Text = wf.NotificationName;
			LinkText.Text = wf.NotificationLink;
			BeginningText.Text = wf.NotificationBeginning;
			EndingText.Text = wf.NotificationEnding;
			FrequencyCombo.SelectedIndex = wf.UpdateFrequenciesListIndex;
			OccurrenceField.Value = wf.NotificationOccurrence;
			EnabledCheck.Checked = true;

			// Пока не будем использовать
			ComparatorFlag.Checked = false;
			ComparatorValue.Text = "";
			MisfitsFlag.Checked = false;
			CheckAvailability.Checked = false;

			UpdateItem (-1);
			UpdateButtons ();
			}

		// Изменение состояния функции
		private void ComparatorFlag_CheckedChanged (object sender, EventArgs e)
			{
			if (ComparatorFlag.Checked)
				ProgramDescription.ShowTip (NSTipTypes.Threshold);

			ComparatorType.Enabled = ComparatorValue.Enabled = MisfitsFlag.Enabled = ComparatorFlag.Checked;
			}

		// Изменение значения компаратора
		private void ComparatorValue_KeyDown (object sender, KeyEventArgs e)
			{
			switch (e.KeyCode)
				{
				case Keys.Up:
				case Keys.Down:
					UpdateComparatorValue (e.KeyCode == Keys.Up);
					break;
				}
			}

		private void ComparatorValue_MouseWheel (object sender, MouseEventArgs e)
			{
			if (e.Delta > 0)
				UpdateComparatorValue (true);
			else if (e.Delta < 0)
				UpdateComparatorValue (false);
			}

		private void UpdateComparatorValue (bool Increase)
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
			}

		// Изменение состояния «включено»
		private void EnabledCheck_CheckedChanged (object sender, EventArgs e)
			{
			EnabledCheck.Text = (EnabledCheck.Checked ? "4" : ";");
			}
		}
	}
