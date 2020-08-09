using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает форму настроек оповещений
	/// </summary>
	public partial class SettingsForm:Form
		{
		// Переменные и константы
		private List<Notification> notifications;
		private string[][] templates = new string[][] { 
			new string[] { "Записи со стены ВК (вариант 1)", "https://vk.com/ID_ИЛИ_НАЗВАНИЕ_ГРУППЫ", "pi_text\">", "</div" },
			new string[] { "Записи со стены ВК (вариант 2)", "https://vk.com/ID_ИЛИ_НАЗВАНИЕ_ГРУППЫ", "pi_text zoom_text\">", "</div" },
			new string[] { "КоммерсантЪ", "https://www.kommersant.ru", "from=hotnews\">", "</h3>" },
			new string[] { "Российская газета", "https://rg.ru", "class=\"b-link__inner-text\">", "</div>" }
			};

		/// <summary>
		/// Конструктор. Настраивает главную форму приложения
		/// </summary>
		/// <param name="Notifications">Список загруженных оповещений</param>
		/// <param name="UpdatingFrequencyStep">Шаг изменения частоты обновления</param>
		public SettingsForm (List<Notification> Notifications, uint UpdatingFrequencyStep)
			{
			// Инициализация
			InitializeComponent ();
			this.Text = ProgramDescription.AssemblyTitle;
			this.CancelButton = BClose;

			for (uint i = 1; i <= 24; i++)
				FrequencyCombo.Items.Add ((i * UpdatingFrequencyStep).ToString () + " минут");
			FrequencyCombo.SelectedIndex = 2;
			EnabledCheck.Checked = true;

			// Загрузка оповещений в список
			notifications = Notifications;

			BAdd.Enabled = (notifications.Count < Notification.MaxNotifications);
			BDelete.Enabled = BUpdate.Enabled = (notifications.Count > 0);

			for (int i = 0; i < notifications.Count; i++)
				NotificationsList.Items.Add (notifications[i].Name + (notifications[i].IsEnabled ? " (вкл)" : " (выкл)"));
			if (NotificationsList.Items.Count > 0)
				NotificationsList.SelectedIndex = 0;

			// Загрузка шаблонов
			for (int i = 0; i < templates.Length; i++)
				TemplatesCombo.Items.Add (templates[i][0]);
			TemplatesCombo.SelectedIndex = 0;

			// Запуск
			this.ShowDialog ();
			}

		// Закрытие окна просмотра
		private void BClose_Click (object sender, EventArgs e)
			{
			// Сохранение оповещений
			Notification.SaveNotifications (notifications);

			// Закрытие окна
			this.Close ();
			}

		// Загрузка значений в поля
		private void NotificationsList_DoubleClick (object sender, EventArgs e)
			{
			// Контроль
			if (NotificationsList.SelectedIndex < 0)
				return;

			// Загрузка
			NameText.Text = notifications[NotificationsList.SelectedIndex].Name;
			LinkText.Text = notifications[NotificationsList.SelectedIndex].Link;
			BeginningText.Text = notifications[NotificationsList.SelectedIndex].Beginning;
			EndingText.Text = notifications[NotificationsList.SelectedIndex].Ending;
			FrequencyCombo.SelectedIndex = (int)notifications[NotificationsList.SelectedIndex].UpdateFrequency - 1;
			EnabledCheck.Checked = notifications[NotificationsList.SelectedIndex].IsEnabled;
			}

		// Добавление и обновление позиций
		private void BAdd_Click (object sender, EventArgs e)
			{
			UpdateItem (-1);
			}

		private void BUpdate_Click (object sender, EventArgs e)
			{
			if (MessageBox.Show ("Заменить выбранное оповещение?", ProgramDescription.AssemblyTitle,
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
				return;

			UpdateItem (NotificationsList.SelectedIndex);
			}

		// Метод обновления оповещений (номер -1 – добавление нового)
		private void UpdateItem (int ItemNumber)
			{
			// Инициализация оповещения
			Notification ni = new Notification (NameText.Text, LinkText.Text, BeginningText.Text, EndingText.Text,
				(uint)(FrequencyCombo.SelectedIndex + 1));
			if (!ni.IsInited)
				{
				MessageBox.Show ("Одно из обязательных полей незаполнено, или ссылка на ресурс указана некорректно",
					ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}
			ni.IsEnabled = EnabledCheck.Checked;

			// Добавление
			if (ItemNumber < 0)
				{
				NotificationsList.Items.Add (ni.Name + (ni.IsEnabled ? " (вкл)" : " (выкл)"));
				notifications.Add (ni);
				}
			else if (ItemNumber < NotificationsList.Items.Count)
				{
				NotificationsList.Items[ItemNumber] = ni.Name + (ni.IsEnabled ? " (вкл)" : " (выкл)");
				notifications[ItemNumber] = ni;
				}
			else
				{
				MessageBox.Show ("Не выбрано оповещение для обновления",
					ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}

			// Обновление контролов
			BAdd.Enabled = (notifications.Count < Notification.MaxNotifications);
			BDelete.Enabled = BUpdate.Enabled = (notifications.Count > 0);
			}

		// Удаление оповещения
		private void BDelete_Click (object sender, EventArgs e)
			{
			// Контроль
			if (NotificationsList.SelectedIndex < 0)
				{
				MessageBox.Show ("Не выбрано оповещение для удаления",
					ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}

			if (MessageBox.Show ("Удалить выбранное оповещение?", ProgramDescription.AssemblyTitle,
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
				return;

			// Удаление
			notifications.RemoveAt (NotificationsList.SelectedIndex);
			NotificationsList.Items.RemoveAt (NotificationsList.SelectedIndex);

			// Обновление контролов
			BAdd.Enabled = (notifications.Count < Notification.MaxNotifications);
			BDelete.Enabled = BUpdate.Enabled = (notifications.Count > 0);
			}

		// Загрузка шаблона в поля
		private void BLoadTemplate_Click (object sender, EventArgs e)
			{
			NameText.Text = templates[TemplatesCombo.SelectedIndex][0];
			LinkText.Text = templates[TemplatesCombo.SelectedIndex][1];
			BeginningText.Text = templates[TemplatesCombo.SelectedIndex][2];
			EndingText.Text = templates[TemplatesCombo.SelectedIndex][3];
			}

		// Автоматизированный поиск ограничителей
		private void FindDelimiters_Click (object sender, EventArgs e)
			{
			// Контроль
			if (BeginningText.Text == "")
				{
				MessageBox.Show ("В поле «Начало» не указано ключевое слово для поиска",
					ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}

			// Поиск
			string beginning = "", ending = "";
			if (!Notification.FindDelimiters (LinkText.Text, BeginningText.Text, out beginning, out ending))
				{
				MessageBox.Show ("Неверно задана ссылка на веб-страницу, или ключевое слово на странице не обнаруживается",
					ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}

			// Успешно
			BeginningText.Text = beginning.Trim ();
			EndingText.Text = ending.Trim ();
			}
		}
	}
