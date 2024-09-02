namespace AC
{
	using Microsoft.Maui.Controls;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;
	using Npgsql;
	using Dapper;

	public partial class Statistics : ContentPage
	{
		private readonly string _role;
		private readonly string _uin;

		// ����������� �� ��������� � �������������� �������� �� ��������� ��� role � uin
		public Statistics() : this("defaultRole", "defaultUIN")
		{
		}

		// �������� ����������� � ����������� role � uin
		public Statistics(string role, string uin)
		{
			InitializeComponent();
			_role = role;
			_uin = uin;
			LoadStatistics();
		}

		private async void OnStatisticsClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Statistics(_role, _uin));  // �������� ����� ����������
		}

		private async void OnDesktopClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Desktop(_role, _uin));  // �������� ����� ����������
		}

		private async void OnProfileClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Profile(_role, _uin));  // �������� ����� ����������
		}

		private async void LoadStatistics()
		{
			if (_role == "student")
			{
				await LoadStudentStatistics();
			}
			else if (_role == "teacher")
			{
				await LoadTeacherStatistics();
			}
		}

		private async Task LoadStudentStatistics()
		{
			const string connectionString = "Host=localhost;Port=5433;Username=postgres;Password=1234;Database=my_database";

			using (var connection = new NpgsqlConnection(connectionString))
			{
				await connection.OpenAsync();

				string query = @"
                SELECT l.teacheruin AS TeacherUIN,
                       CONCAT(u.last_name, ' ', u.first_name, ' ', u.patronymic) AS TeacherName,
                       COUNT(CASE WHEN s.status = 'green' THEN 1 END) AS AttendedClasses,
                       COUNT(CASE WHEN s.status = 'yellow' THEN 1 END) AS LateClasses
                FROM student_attendance s
                INNER JOIN lessons l ON s.lessonid = l.lessonid
                INNER JOIN users u ON l.teacheruin = u.uin
                WHERE s.uin = @UIN
                GROUP BY l.teacheruin, u.last_name, u.first_name, u.patronymic";

				var studentStats = await connection.QueryAsync<StudentStatisticsItem>(query, new { UIN = _uin });

				StudentStatisticsCollectionView.ItemsSource = studentStats.ToList();
				StudentStatisticsLayout.IsVisible = true;
				TeacherStatisticsLayout.IsVisible = false;
			}
		}

		private async Task LoadTeacherStatistics()
		{
			const string connectionString = "Host=localhost;Port=5433;Username=postgres;Password=1234;Database=my_database";

			using (var connection = new NpgsqlConnection(connectionString))
			{
				await connection.OpenAsync();

				string lessonsQuery = "SELECT COUNT(*) FROM lessons WHERE teacheruin = @UIN";
				int createdLessons = await connection.ExecuteScalarAsync<int>(lessonsQuery, new { UIN = _uin });

				string studentsQuery = @"
                SELECT COUNT(DISTINCT s.uin)
                FROM lessons l
                INNER JOIN student_attendance s ON l.lessonid = s.lessonid
                WHERE l.teacheruin = @UIN";
				int studentsInLessons = await connection.ExecuteScalarAsync<int>(studentsQuery, new { UIN = _uin });

				CreatedLessonsLabel.Text = $"�����: {createdLessons}";
				StudentsInLessonsLabel.Text = $"��������: {studentsInLessons}";
				TeacherStatisticsLayout.IsVisible = true;
				StudentStatisticsLayout.IsVisible = false;
			}
		}

		public class StudentStatisticsItem
		{
			public string TeacherName { get; set; }
			public int AttendedClasses { get; set; }
			public int LateClasses { get; set; }
		}
	}
}
