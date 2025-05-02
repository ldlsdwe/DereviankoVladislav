using System;
using System.Collections.Generic;
using System.IO;
using BCrypt.Net;


record User(string Email, string PasswordHash, bool IsAdmin = false)
{
    public HashSet<int> Favorites { get; } = new();

    public static User Create(string email, string plainPass, bool admin = false) =>
        new(email, BCrypt.HashPassword(plainPass), admin);

    public bool CheckPassword(string plainPass) =>
        BCrypt.Verify(plainPass, PasswordHash);
}

record Book
{
    public int    Id    { get; init; }
    public string Title { get; init; } = string.Empty;
}


class Program
{
    static readonly List<User> Users = new();
    static readonly List<Book> Books = new();
    static          User? Current   = null;
    static          int   nextId    = 1;

    static void Main()
    {
        SeedAdmin();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== ОНЛАЙН-БІБЛІОТЕКА ===");

            if (Current == null) GuestMenu();
            else                 UserMenu();
        }
    }

    static void GuestMenu()
    {
        Console.WriteLine("1) Реєстрація");
        Console.WriteLine("2) Вхід");
        Console.WriteLine("0) Вихід");
        Console.Write("> ");
        switch (Console.ReadLine())
        {
            case "1": Register(); break;
            case "2": Login();    break;
            case "0": Environment.Exit(0); break;
        }
    }

    static void UserMenu()
    {
        Console.WriteLine($"\nКористувач: {Current!.Email} ({Role(Current)})");
        Console.WriteLine("3) Список книжок");
        Console.WriteLine("4) Додати книжку              (admin)");
        Console.WriteLine("5) Видалити книжку            (admin)");
        Console.WriteLine("6) Додати у вибране");
        Console.WriteLine("7) Прибрати з вибраного");
        Console.WriteLine("8) Експорт CSV                (admin)");
        Console.WriteLine("9) Змінити роль користувача   (admin)");
        Console.WriteLine("L) Вихід з облікового запису");
        Console.Write("> ");

        switch (Console.ReadLine()?.ToUpperInvariant())
        {
            case "3": ListBooks();              break;
            case "4": if (IsAdmin()) AddBook(); break;
            case "5": if (IsAdmin()) RemoveBook(); break;
            case "6": AddFavourite();           break;
            case "7": RemoveFavourite();        break;
            case "8": if (IsAdmin()) Export();  break;
            case "9": if (IsAdmin()) Promote(); break;
            case "L": Current = null;           break;
        }
        Pause();
    }

    static void Register()
    {
        Console.Write("E-mail: "); var email = Console.ReadLine()!;
        if (Users.Exists(u => u.Email == email))
        {
            Console.WriteLine("Такий e-mail уже існує.");
            Pause(); return;
        }
        Console.Write("Пароль: "); var pw1 = ReadHidden();
        Console.Write("Повторіть пароль: "); var pw2 = ReadHidden();
        if (pw1 != pw2) { Console.WriteLine("Паролі не співпадають!"); Pause(); return; }

        Users.Add(User.Create(email, pw1));
        Console.WriteLine("Реєстрація успішна!");
        Pause();
    }

    static void Login()
    {
        Console.Write("E-mail: "); var email = Console.ReadLine()!;
        Console.Write("Пароль: "); var pass = ReadHidden();
        Current = Users.Find(u => u.Email == email && u.CheckPassword(pass));
        Console.WriteLine(Current == null ? "Невірні дані." : "Вхід успішний.");
        Pause();
    }

    static void ListBooks()
    {
        if (Books.Count == 0) { Console.WriteLine("Книжок нема."); return; }
        foreach (var b in Books) Console.WriteLine($"{b.Id}. {b.Title}");
    }

    static void AddBook()
    {
        Console.Write("Назва книжки: ");
        Books.Add(new Book { Id = nextId++, Title = Console.ReadLine()! });
        Console.WriteLine("Додано.");
    }

    static void RemoveBook()
    {
        Console.Write("ID: ");
        if (int.TryParse(Console.ReadLine(), out var id) &&
            Books.RemoveAll(b => b.Id == id) > 0)
            Console.WriteLine("Видалено.");
        else Console.WriteLine("Не знайдено.");
    }

    static void AddFavourite()
    {
        Console.Write("ID книжки: ");
        if (int.TryParse(Console.ReadLine(), out var id) && Books.Exists(b => b.Id == id))
        {
            Current!.Favorites.Add(id);
            Console.WriteLine("Додано у вибране.");
        }
        else Console.WriteLine("Книжка не знайдена.");
    }

    static void RemoveFavourite()
    {
        Console.Write("ID книжки: ");
        if (int.TryParse(Console.ReadLine(), out var id) && Current!.Favorites.Remove(id))
            Console.WriteLine("Прибрано з вибраного.");
        else Console.WriteLine("Цієї книжки нема у вибраному.");
    }

    static void Export()
    {
        File.WriteAllLines("books.csv", Books.ConvertAll(b => $"{b.Id},{b.Title}"));
        Console.WriteLine("Файл books.csv створено.");
    }

    static void Promote()
    {
        Console.Write("E-mail користувача: ");
        var email = Console.ReadLine()!;
        var u = Users.Find(x => x.Email == email);
        if (u == null) { Console.WriteLine("Користувача не знайдено."); return; }

        u.IsAdmin = !u.IsAdmin;
        Console.WriteLine($"Тепер роль: {Role(u)}");
    }

    static void SeedAdmin() =>
        Users.Add(User.Create("admin@lib.local", "admin", admin: true));

    static bool   IsAdmin() => Current?.IsAdmin ?? false;
    static string Role(User u) => u.IsAdmin ? "адмін" : "клієнт";
    static void   Pause() { Console.WriteLine("Натисніть Enter…"); Console.ReadLine(); }

    static string ReadHidden()
    {
        var pwd = string.Empty;
        ConsoleKey key;
        while ((key = Console.ReadKey(true).Key) is not ConsoleKey.Enter)
        {
            if (key == ConsoleKey.Backspace && pwd.Length > 0)
            { pwd = pwd[0..^1]; Console.Write("\b \b"); }
            else if (!char.IsControl((char)key))
            { pwd += (char)key; Console.Write('*'); }
        }
        Console.WriteLine();
        return pwd;
    }
}
