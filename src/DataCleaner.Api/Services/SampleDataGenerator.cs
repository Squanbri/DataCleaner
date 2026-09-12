using System.Globalization;
using System.Text;

namespace DataCleaner.Api.Services;

public sealed class SampleDataGenerator
{
    private static readonly string[] FirstNames =
    [
        "Александр", "Алексей", "Андрей", "Антон", "Артём", "Борис", "Вадим", "Валентин",
        "Василий", "Виктор", "Владимир", "Глеб", "Григорий", "Даниил", "Денис", "Дмитрий",
        "Евгений", "Егор", "Иван", "Игорь", "Илья", "Кирилл", "Константин", "Лев",
        "Максим", "Михаил", "Никита", "Николай", "Олег", "Павел", "Пётр", "Роман",
        "Сергей", "Станислав", "Тимофей", "Фёдор", "Юрий", "Ярослав",
        "Анна", "Валентина", "Вера", "Виктория", "Галина", "Дарья", "Екатерина", "Елена",
        "Ирина", "Ксения", "Людмила", "Мария", "Наталья", "Ольга", "Полина", "Светлана",
        "София", "Татьяна", "Юлия",
    ];

    private static readonly string[] MiddleNamesM =
    [
        "Александрович", "Алексеевич", "Андреевич", "Борисович", "Васильевич",
        "Викторович", "Владимирович", "Дмитриевич", "Евгеньевич", "Иванович",
        "Игоревич", "Михайлович", "Николаевич", "Олегович", "Павлович",
        "Петрович", "Сергеевич", "Юрьевич",
    ];

    private static readonly string[] MiddleNamesF =
    [
        "Александровна", "Алексеевна", "Андреевна", "Борисовна", "Васильевна",
        "Викторовна", "Владимировна", "Дмитриевна", "Евгеньевна", "Ивановна",
        "Игоревна", "Михайловна", "Николаевна", "Олеговна", "Павловна",
        "Петровна", "Сергеевна", "Юрьевна",
    ];

    private static readonly string[] LastNames =
    [
        "Иванов", "Смирнов", "Кузнецов", "Попов", "Васильев", "Петров", "Соколов",
        "Михайлов", "Новиков", "Фёдоров", "Морозов", "Волков", "Алексеев", "Лебедев",
        "Семёнов", "Егоров", "Павлов", "Козлов", "Степанов", "Николаев", "Орлов",
        "Андреев", "Макаров", "Никитин", "Захаров", "Зайцев", "Соловьёв", "Борисов",
        "Яковлев", "Григорьев", "Романов", "Воробьёв", "Сергеев", "Фролов", "Александров",
        "Петров-Водкин", "Римский-Корсаков",
    ];

    private static readonly string[] Cities =
    [
        "Москва", "Санкт-Петербург", "Казань", "Новосибирск", "Екатеринбург",
        "Нижний Новгород", "Самара", "Омск", "Ростов-на-Дону", "Уфа",
        "Красноярск", "Воронеж", "Пермь", "Волгоград", "Краснодар",
    ];

    private static readonly HashSet<string> FemaleFirst = new(StringComparer.Ordinal)
    {
        "Анна", "Валентина", "Вера", "Виктория", "Галина", "Дарья", "Екатерина", "Елена",
        "Ирина", "Ксения", "Людмила", "Мария", "Наталья", "Ольга", "Полина", "Светлана",
        "София", "Татьяна", "Юлия",
    };

    public GeneratedSampleSet Generate(SampleGenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var rng = new Mulberry32(unchecked((uint)options.Seed));
        var people = new List<Person>(options.BasePeople);
        for (var i = 1; i <= options.BasePeople; i++)
        {
            people.Add(MakePerson(rng, i));
        }

        var rows = new List<SampleRow>();
        var externalSeq = 1;

        foreach (var person in people)
        {
            rows.Add(new SampleRow(
                $"crm-{externalSeq++}",
                person.FullName,
                person.Phone,
                person.Email,
                person.Birth.Iso,
                person.City,
                person.Id));

            if (rng.NextDouble() >= options.DirtyCopyRate)
            {
                continue;
            }

            var copies = 1 + (int)Math.Floor(rng.NextDouble() * 3);
            for (var c = 0; c < copies; c++)
            {
                var dropPhone = rng.NextDouble() < 0.25;
                var dropEmail = !dropPhone && rng.NextDouble() < 0.25;
                rows.Add(new SampleRow(
                    $"crm-{externalSeq++}",
                    DirtyName(rng, person),
                    dropPhone ? "" : FormatPhoneDirty(rng, person.PhoneBody),
                    dropEmail ? "" : DirtyEmail(rng, person.Email),
                    DirtyBirth(rng, person.Birth),
                    DirtyCity(rng, person.City),
                    person.Id));
            }
        }

        var garbageCount = (int)Math.Floor(options.BasePeople * options.GarbageRate);
        for (var i = 0; i < garbageCount; i++)
        {
            var truthId = 100_000 + i;
            var kind = (int)Math.Floor(rng.NextDouble() * 4);
            rows.Add(new SampleRow(
                $"crm-{externalSeq++}",
                kind == 0 ? "" : "???",
                kind == 1 ? "12345" : "",
                kind == 2 ? "not-an-email" : "",
                kind == 3 ? "31.02.1990" : "99.99.9999",
                "",
                truthId));
        }

        for (var i = rows.Count - 1; i > 0; i--)
        {
            var j = (int)Math.Floor(rng.NextDouble() * (i + 1));
            (rows[i], rows[j]) = (rows[j], rows[i]);
        }

        var customersCsv = BuildCustomersCsv(rows);
        var truthCsv = BuildTruthCsv(rows);
        var duplicateGroups = CountTruthGroups(rows);

        return new GeneratedSampleSet(
            customersCsv,
            truthCsv,
            rows.Count,
            duplicateGroups,
            options);
    }

    private static string BuildCustomersCsv(IReadOnlyList<SampleRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("external_id;full_name;phone;email;birth_date;city");
        foreach (var row in rows)
        {
            sb.Append(Escape(row.ExternalId)).Append(';')
                .Append(Escape(row.FullName)).Append(';')
                .Append(Escape(row.Phone)).Append(';')
                .Append(Escape(row.Email)).Append(';')
                .Append(Escape(row.BirthDate)).Append(';')
                .Append(Escape(row.City))
                .AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildTruthCsv(IReadOnlyList<SampleRow> rows)
    {
        var groups = new SortedDictionary<int, List<int>>();
        for (var i = 0; i < rows.Count; i++)
        {
            var truthId = rows[i].TruthId;
            if (!groups.TryGetValue(truthId, out var list))
            {
                list = [];
                groups[truthId] = list;
            }

            list.Add(i + 1);
        }

        var sb = new StringBuilder();
        sb.AppendLine("truth_id;row_numbers");
        foreach (var (truthId, rowNumbers) in groups)
        {
            if (rowNumbers.Count < 2)
            {
                continue;
            }

            sb.Append(truthId).Append(';').Append(string.Join(',', rowNumbers)).AppendLine();
        }

        return sb.ToString();
    }

    private static int CountTruthGroups(IReadOnlyList<SampleRow> rows)
    {
        return rows.GroupBy(r => r.TruthId).Count(g => g.Count() > 1);
    }

    private static Person MakePerson(Mulberry32 rng, int id)
    {
        var first = Pick(rng, FirstNames);
        var isFemale = FemaleFirst.Contains(first);
        var last = Pick(rng, LastNames);
        if (isFemale && !last.Contains('-', StringComparison.Ordinal))
        {
            last += "а";
        }

        var middle = Pick(rng, isFemale ? MiddleNamesF : MiddleNamesM);
        var birth = RandomBirth(rng);
        var phoneBody = PhoneDigits(rng);
        return new Person(
            id,
            last,
            first,
            middle,
            $"{last} {first} {middle}",
            phoneBody,
            "+7" + phoneBody,
            EmailLocal(first, id),
            birth,
            Pick(rng, Cities));
    }

    private static Birth RandomBirth(Mulberry32 rng)
    {
        var year = 1955 + (int)Math.Floor(rng.NextDouble() * 45);
        var month = 1 + (int)Math.Floor(rng.NextDouble() * 12);
        var day = 1 + (int)Math.Floor(rng.NextDouble() * 28);
        return new Birth(year, month, day, $"{year}-{Pad(month)}-{Pad(day)}");
    }

    private static string PhoneDigits(Mulberry32 rng)
    {
        var body = new StringBuilder(10);
        body.Append('9');
        for (var i = 0; i < 9; i++)
        {
            body.Append((int)Math.Floor(rng.NextDouble() * 10));
        }

        return body.ToString();
    }

    private static string EmailLocal(string first, int id)
    {
        var local = new string(first.ToLowerInvariant()
            .Replace('ё', 'е')
            .Where(char.IsLetterOrDigit)
            .Take(4)
            .ToArray());
        if (string.IsNullOrEmpty(local))
        {
            local = "x";
        }

        return $"user{id}.{local}@mail.ru";
    }

    private static string FormatPhoneDirty(Mulberry32 rng, string body10)
    {
        var variant = (int)Math.Floor(rng.NextDouble() * 5);
        return variant switch
        {
            0 => $"8 ({body10[..3]}) {body10[3..6]}-{body10[6..8]}-{body10[8..]}",
            1 => $"+7{body10}",
            2 => $"{body10[..3]} {body10[3..6]} {body10[6..8]} {body10[8..]}",
            3 => $"8-{body10[..3]}-{body10}",
            _ => $"7{body10}",
        };
    }

    private static string DirtyName(Mulberry32 rng, Person person)
    {
        var roll = rng.NextDouble();
        if (roll < 0.25) return person.FullName.ToUpper(CultureInfo.GetCultureInfo("ru-RU"));
        if (roll < 0.4) return person.FullName.ToLower(CultureInfo.GetCultureInfo("ru-RU"));
        if (roll < 0.55) return person.FullName.Replace(" ", "  ", StringComparison.Ordinal);
        if (roll < 0.7) return $"{person.First} {person.Last}";
        if (roll < 0.85) return $"{person.Last} {person.First}";
        return person.FullName
            .Replace("е", "ё", StringComparison.Ordinal)
            .Replace("Е", "Ё", StringComparison.Ordinal);
    }

    private static string DirtyEmail(Mulberry32 rng, string email)
        => rng.NextDouble() < 0.5 ? $" {email.ToUpperInvariant()} " : email.ToUpperInvariant();

    private static string DirtyBirth(Mulberry32 rng, Birth birth)
    {
        var variant = (int)Math.Floor(rng.NextDouble() * 4);
        return variant switch
        {
            0 => $"{Pad(birth.Day)}.{Pad(birth.Month)}.{birth.Year}",
            1 => birth.Iso,
            2 => $"{Pad(birth.Day)}/{Pad(birth.Month)}/{birth.Year}",
            _ => $"{birth.Day}.{birth.Month}.{birth.Year}",
        };
    }

    private static string DirtyCity(Mulberry32 rng, string city)
    {
        var roll = rng.NextDouble();
        if (roll < 0.4) return $"г. {city}";
        if (roll < 0.7) return city.ToUpper(CultureInfo.GetCultureInfo("ru-RU"));
        return $"{city} г.";
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        if (value.IndexOfAny([';', '"', '\n', '\r']) >= 0)
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }

    private static string Pad(int n) => n.ToString("00", CultureInfo.InvariantCulture);

    private static T Pick<T>(Mulberry32 rng, IReadOnlyList<T> items)
        => items[(int)Math.Floor(rng.NextDouble() * items.Count)];

    private sealed class Mulberry32(uint seed)
    {
        private uint _state = seed;

        public double NextDouble()
        {
            unchecked
            {
                _state += 0x6D2B79F5;
                var t = _state;
                t = (uint)(Imul((int)(t ^ (t >> 15)), (int)(1 | t)));
                t ^= t + (uint)Imul((int)(t ^ (t >> 7)), (int)(61 | t));
                _state = t ^ (t >> 14);
                return _state / 4294967296.0;
            }
        }

        private static int Imul(int a, int b) => unchecked(a * b);
    }

    private sealed record Birth(int Year, int Month, int Day, string Iso);

    private sealed record Person(
        int Id,
        string Last,
        string First,
        string Middle,
        string FullName,
        string PhoneBody,
        string Phone,
        string Email,
        Birth Birth,
        string City);

    private sealed record SampleRow(
        string ExternalId,
        string FullName,
        string Phone,
        string Email,
        string BirthDate,
        string City,
        int TruthId);
}

public sealed class SampleGenerationOptions
{
    public int BasePeople { get; set; } = 500;
    public double DirtyCopyRate { get; set; } = 0.2;
    public double GarbageRate { get; set; } = 0.03;
    public int Seed { get; set; } = 20260912;

    public void Validate()
    {
        if (BasePeople is < 10 or > 5000)
        {
            throw new ArgumentOutOfRangeException(nameof(BasePeople), "BasePeople must be between 10 and 5000.");
        }

        if (DirtyCopyRate is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(DirtyCopyRate));
        }

        if (GarbageRate is < 0 or > 0.2)
        {
            throw new ArgumentOutOfRangeException(nameof(GarbageRate));
        }
    }
}

public sealed record GeneratedSampleSet(
    string CustomersCsv,
    string TruthCsv,
    int TotalRows,
    int DuplicateTruthGroups,
    SampleGenerationOptions Options);
