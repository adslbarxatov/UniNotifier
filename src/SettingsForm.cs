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

			ofd = new OpenFileDialog ();
			sfd = new SaveFileDialog ();
			ofd.Filter = sfd.Filter = Localization.GetText (NotificationsSet.SettingsFileExtension + "file") + "|" +
				NotificationsSet.SettingsFileName;
			ofd.Title = sfd.Title = ProgramDescription.AssemblyVisibleName;
			ofd.CheckFileExists = ofd.CheckPathExists = true;
			sfd.OverwritePrompt = true;
			ofd.Multiselect = false;
			ofd.RestoreDirectory = sfd.RestoreDirectory = true;
			ofd.ShowHelp = ofd.ShowReadOnly = sfd.ShowHelp = false;

			LanguageCombo.Items.AddRange (Localization.LanguagesNames);
			try
				{
				LanguageCombo.SelectedIndex = (int)Localization.CurrentLanguage;
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

			// Загрузка оповещений в список
			UpdateButtons ();

			for (int i = 0; i < notifications.Notifications.Count; i++)
				NotificationsList.Items.Add (notifications.Notifications[i].Name +
					(notifications.Notifications[i].IsEnabled ? " (+)" : " (–)"));
			if (NotificationsList.Items.Count > 0)
				NotificationsList.SelectedIndex = 0;

			// Запуск
			ProgramDescription.ShowTips (ProgramDescription.TipTypes.StartupTip);
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
			// Сохранение оповещений
			notifications.SaveNotifications ();
			callWindowOnUrgents = WindowCallFlag.Checked;

			// Закрытие окна
			ProgramDescription.ShowTips (ProgramDescription.TipTypes.ServiceLaunchTip);
			this.Close ();
			}

		// Загрузка значений в поля
		private void NotificationsList_Select (object sender, EventArgs e)
			{
			// Контроль
			if (NotificationsList.SelectedIndex < 0)
				return;
			else if (NotificationsList.SelectedIndex != 0)
				ProgramDescription.ShowTips (ProgramDescription.TipTypes.CurrentNotButton);

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
			/*ComparatorValue.Value = (decimal)notifications.Notifications[i].ComparisonValue;*/
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
			ProgramDescription.ShowTips (ProgramDescription.TipTypes.AddButton);
			UpdateItem (-1);

			// Обновление кнопок
			UpdateButtons ();
			}

		private void BUpdate_Click (object sender, EventArgs e)
			{
			ProgramDescription.ShowTips (ProgramDescription.TipTypes.ApplyButton);
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
			/*cfg.ComparisonValue = (double)ComparatorValue.Value;*/
			cfg.ComparisonString = ComparatorValue.Text;
			cfg.IgnoreComparisonMisfits = MisfitsFlag.Checked;
			cfg.NotifyWhenUnavailable = CheckAvailability.Checked;

			Notification ni = new Notification (cfg);

			if (!ni.IsInited)
				{
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning, "NotEnoughDataMessage");
				return;
				}

			// Условие не выполняется только в двух случаях:
			// - когда добавляется новое оповещение, не имеющее аналогов в списке;
			// - когда обновляется текущее выбранное оповещение.
			// Остальные случаи следует считать попыткой задвоения имени
			int idx = notifications.Notifications.IndexOf (ni);
			if ((idx >= 0) && (idx != ItemNumber))
				{
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning, "NotMatchingNames");
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
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning, "UpdateLineNotSpecified");
				return;
				}

			// Переключение на новую позицию в случае добавления
			if (ItemNumber < 0)
				NotificationsList.SelectedIndex = NotificationsList.Items.Count - 1;
			}

		// Удаление оповещения
		private void BDelete_Click (object sender, EventArgs e)
			{
			// Контроль
			ProgramDescription.ShowTips (ProgramDescription.TipTypes.DeleteButton);
			if (NotificationsList.SelectedIndex < 0)
				{
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning, "DeleteLineNotSpecified");
				return;
				}

			if (RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning, "DeleteMessage",
				LzDefaultTextValues.Button_YesNoFocus, LzDefaultTextValues.Button_No) ==
				RDMessageButtons.ButtonTwo)
				return;

			// Удаление
			int index = NotificationsList.SelectedIndex;
			notifications.Notifications.RemoveAt (index);
			NotificationsList.Items.RemoveAt (index);

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
			Localization.CurrentLanguage = (SupportedLanguages)LanguageCombo.SelectedIndex;

			// Локализация
			Localization.SetControlsText (this);
			BDelete.Text = Localization.GetDefaultText (LzDefaultTextValues.Button_Delete);
			BUpdate.Text = Localization.GetDefaultText (LzDefaultTextValues.Button_Update);

			int idx = ComparatorType.SelectedIndex;
			char[] ctSplitter = new char[] { '\n' };

			ComparatorType.Items.Clear ();
			ComparatorType.Items.AddRange (Localization.GetText ("ComparatorTypes").Split (ctSplitter));

			if (idx >= 0)
				ComparatorType.SelectedIndex = idx;
			else
				ComparatorType.SelectedIndex = 0;
			}

		// Подсказка по полю Occurence
		private void OccurrenceField_Click (object sender, EventArgs e)
			{
			ProgramDescription.ShowTips (ProgramDescription.TipTypes.OccurenceTip);
			}

		// Выгрузка настроек в буфер обмена
		private void ShareSettings_Click (object sender, EventArgs e)
			{
			// Подсказка
			ProgramDescription.ShowTips (ProgramDescription.TipTypes.ShareSettings);

			// Выбор варианта выгрузки
			switch (RDGenerics.MessageBox (RDMessageTypes.Question, Localization.GetText ("ShareVariant"),
				Localization.GetText ("ShareFile"), Localization.GetText ("ShareClipboard"),
				Localization.GetDefaultText (LzDefaultTextValues.Button_Cancel)))
				{
				// Сохранение в файл
				case RDMessageButtons.ButtonOne:
					// Запрос пути
					sfd.FileName = NotificationsSet.SettingsFileName;
					if (sfd.ShowDialog () != DialogResult.OK)
						return;

					// Сохранение
					string settings = notifications.GetSettingsList ();
					try
						{
						File.WriteAllBytes (sfd.FileName,
							RDGenerics.GetEncoding (SupportedEncodings.Unicode16).GetBytes (settings));
						}
					catch
						{
						RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning, "ShareFailure");
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

				if (RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning, "LoadingWarning",
					LzDefaultTextValues.Button_YesNoFocus, LzDefaultTextValues.Button_No) !=
					RDMessageButtons.ButtonOne)
					return;

				string settings;
				try
					{
					settings = File.ReadAllText (ofd.FileName,
						RDGenerics.GetEncoding (SupportedEncodings.Unicode16));
					}
				catch
					{
					RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning, "LoadingFailure");
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
			/*ComparatorValue.Value = 0;*/
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
				ProgramDescription.ShowTips (ProgramDescription.TipTypes.Threshold);

			ComparatorType.Enabled = ComparatorValue.Enabled = MisfitsFlag.Enabled = ComparatorFlag.Checked;
			}

		// Изменение значения компаратора
		private void ComparatorValue_KeyDown (object sender, KeyEventArgs e)
			{
			switch (e.KeyCode)
				{
				case Keys.Up:
				case Keys.Down:
					double v = 0.0;
					try
						{
						v = double.Parse (ComparatorValue.Text.Replace (',', '.'),
							Localization.GetCulture (SupportedLanguages.en_us));
						}
					catch { }

					if (e.KeyCode == Keys.Up)
						v += 1.0;
					else
						v -= 1.0;

					ComparatorValue.Text = v.ToString (Localization.GetCulture (SupportedLanguages.en_us));
					break;
				}
			}

		// Изменение состояния «включено»
		private void EnabledCheck_CheckedChanged (object sender, EventArgs e)
			{
			EnabledCheck.Text = (EnabledCheck.Checked ? "4" : ";");
			}
		}
	}
