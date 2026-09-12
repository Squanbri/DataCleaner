#!/usr/bin/env node
/**
 * Generates samples/customers.csv (dirty CRM export) and samples/customers.truth.csv
 * (ground-truth duplicate groups for later evaluation).
 *
 * Usage: node tools/generate-customers.mjs
 */

import { writeFileSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = join(__dirname, "..");
const samplesDir = join(root, "samples");

const FIRST_NAMES = [
  "Александр", "Алексей", "Андрей", "Антон", "Артём", "Борис", "Вадим", "Валентин",
  "Василий", "Виктор", "Владимир", "Глеб", "Григорий", "Даниил", "Денис", "Дмитрий",
  "Евгений", "Егор", "Иван", "Игорь", "Илья", "Кирилл", "Константин", "Лев",
  "Максим", "Михаил", "Никита", "Николай", "Олег", "Павел", "Пётр", "Роман",
  "Сергей", "Станислав", "Тимофей", "Фёдор", "Юрий", "Ярослав",
  "Анна", "Валентина", "Вера", "Виктория", "Галина", "Дарья", "Екатерина", "Елена",
  "Ирина", "Ксения", "Людмила", "Мария", "Наталья", "Ольга", "Полина", "Светлана",
  "София", "Татьяна", "Юлия",
];

const MIDDLE_NAMES_M = [
  "Александрович", "Алексеевич", "Андреевич", "Борисович", "Васильевич",
  "Викторович", "Владимирович", "Дмитриевич", "Евгеньевич", "Иванович",
  "Игоревич", "Михайлович", "Николаевич", "Олегович", "Павлович",
  "Петрович", "Сергеевич", "Юрьевич",
];

const MIDDLE_NAMES_F = [
  "Александровна", "Алексеевна", "Андреевна", "Борисовна", "Васильевна",
  "Викторовна", "Владимировна", "Дмитриевна", "Евгеньевна", "Ивановна",
  "Игоревна", "Михайловна", "Николаевна", "Олеговна", "Павловна",
  "Петровна", "Сергеевна", "Юрьевна",
];

const LAST_NAMES = [
  "Иванов", "Смирнов", "Кузнецов", "Попов", "Васильев", "Петров", "Соколов",
  "Михайлов", "Новиков", "Фёдоров", "Морозов", "Волков", "Алексеев", "Лебедев",
  "Семёнов", "Егоров", "Павлов", "Козлов", "Степанов", "Николаев", "Орлов",
  "Андреев", "Макаров", "Никитин", "Захаров", "Зайцев", "Соловьёв", "Борисов",
  "Яковлев", "Григорьев", "Романов", "Воробьёв", "Сергеев", "Фролов", "Александров",
  "Петров-Водкин", "Римский-Корсаков",
];

const CITIES = [
  "Москва", "Санкт-Петербург", "Казань", "Новосибирск", "Екатеринбург",
  "Нижний Новгород", "Самара", "Омск", "Ростов-на-Дону", "Уфа",
  "Красноярск", "Воронеж", "Пермь", "Волгоград", "Краснодар",
];

const FEMALE_FIRST = new Set([
  "Анна", "Валентина", "Вера", "Виктория", "Галина", "Дарья", "Екатерина", "Елена",
  "Ирина", "Ксения", "Людмила", "Мария", "Наталья", "Ольга", "Полина", "Светлана",
  "София", "Татьяна", "Юлия",
]);

/** Deterministic RNG (mulberry32). */
function createRng(seed) {
  let t = seed >>> 0;
  return () => {
    t += 0x6d2b79f5;
    let r = Math.imul(t ^ (t >>> 15), 1 | t);
    r ^= r + Math.imul(r ^ (r >>> 7), 61 | r);
    return ((r ^ (r >>> 14)) >>> 0) / 4294967296;
  };
}

function pick(rng, arr) {
  return arr[Math.floor(rng() * arr.length)];
}

function pad(n, w = 2) {
  return String(n).padStart(w, "0");
}

function randomBirth(rng) {
  const year = 1955 + Math.floor(rng() * 45);
  const month = 1 + Math.floor(rng() * 12);
  const day = 1 + Math.floor(rng() * 28);
  return { year, month, day, iso: `${year}-${pad(month)}-${pad(day)}` };
}

function phoneDigits(rng) {
  // 9XXXXXXXXX mobile body
  let body = "9";
  for (let i = 0; i < 9; i++) body += Math.floor(rng() * 10);
  return body;
}

function toE164(body10) {
  return `+7${body10}`;
}

function emailLocal(first, last, id) {
  const map = { ё: "e", Ё: "E" };
  const translit = (s) =>
    s
      .toLowerCase()
      .replace(/[ёЁ]/g, (c) => map[c] || c)
      .replace(/[^a-zа-я0-9]+/gi, ".")
      .replace(/^\.+|\.+$/g, "");
  // keep Cyrillic locals simple for realism in dirty data — use latin-ish from id
  return `user${id}.${translit(first).slice(0, 4) || "x"}@mail.ru`.replace(/\.+/g, ".");
}

function makePerson(rng, id) {
  const first = pick(rng, FIRST_NAMES);
  const isFemale = FEMALE_FIRST.has(first);
  let last = pick(rng, LAST_NAMES);
  if (isFemale && !last.includes("-")) last = last + "а";
  const middle = pick(rng, isFemale ? MIDDLE_NAMES_F : MIDDLE_NAMES_M);
  const birth = randomBirth(rng);
  const phoneBody = phoneDigits(rng);
  const city = pick(rng, CITIES);
  return {
    id,
    last,
    first,
    middle,
    fullName: `${last} ${first} ${middle}`,
    phoneBody,
    phone: toE164(phoneBody),
    email: emailLocal(first, last, id),
    birth,
    city,
  };
}

function formatPhoneDirty(rng, body10) {
  const variants = [
    () => `8 (${body10.slice(0, 3)}) ${body10.slice(3, 6)}-${body10.slice(6, 8)}-${body10.slice(8)}`,
    () => `+7${body10}`,
    () => `${body10.slice(0, 3)} ${body10.slice(3, 6)} ${body10.slice(6, 8)} ${body10.slice(8)}`,
    () => `8-${body10.slice(0, 3)}-${body10}`,
    () => `7${body10}`,
  ];
  return pick(rng, variants)();
}

function dirtyName(rng, person) {
  const roll = rng();
  if (roll < 0.25) return person.fullName.toUpperCase();
  if (roll < 0.4) return person.fullName.toLowerCase();
  if (roll < 0.55) return person.fullName.replace(/ /g, "  ");
  if (roll < 0.7) return `${person.first} ${person.last}`;
  if (roll < 0.85) return `${person.last} ${person.first}`;
  // ё/е swap or typo
  return person.fullName.replace(/е/gi, (c) => (c === "е" ? "ё" : "Ё"));
}

function dirtyEmail(rng, email) {
  const roll = rng();
  if (roll < 0.5) return ` ${email.toUpperCase()} `;
  return email.toUpperCase();
}

function dirtyBirth(rng, birth) {
  const variants = [
    () => `${pad(birth.day)}.${pad(birth.month)}.${birth.year}`,
    () => birth.iso,
    () => `${pad(birth.day)}/${pad(birth.month)}/${birth.year}`,
    () => `${birth.day}.${birth.month}.${birth.year}`,
  ];
  return pick(rng, variants)();
}

function dirtyCity(rng, city) {
  const roll = rng();
  if (roll < 0.4) return `г. ${city}`;
  if (roll < 0.7) return city.toUpperCase();
  return `${city} г.`;
}

function csvEscape(value) {
  if (value == null) return "";
  const s = String(value);
  if (/[;"\n\r]/.test(s)) return `"${s.replace(/"/g, '""')}"`;
  return s;
}

function rowToLine(row) {
  return [
    row.external_id,
    row.full_name,
    row.phone,
    row.email,
    row.birth_date,
    row.city,
  ]
    .map(csvEscape)
    .join(";");
}

function main() {
  const rng = createRng(20260912);
  const people = [];
  const TARGET_BASE = 3000;

  for (let i = 1; i <= TARGET_BASE; i++) {
    people.push(makePerson(rng, i));
  }

  /** @type {{external_id:string, full_name:string, phone:string, email:string, birth_date:string, city:string, truth_id:number}[]} */
  const rows = [];
  /** @type {Map<number, number[]>} truth group -> row numbers (1-based after header) */
  const truth = new Map();

  let externalSeq = 1;

  function pushRow(fields, truthId) {
    rows.push({ ...fields, truth_id: truthId });
    if (!truth.has(truthId)) truth.set(truthId, []);
    // row number assigned after shuffle — store temporary index
  }

  for (const person of people) {
    const clean = {
      external_id: `crm-${externalSeq++}`,
      full_name: person.fullName,
      phone: person.phone,
      email: person.email,
      birth_date: person.birth.iso,
      city: person.city,
    };
    pushRow(clean, person.id);

    // ~20% get 1–3 dirty copies
    if (rng() < 0.2) {
      const copies = 1 + Math.floor(rng() * 3);
      for (let c = 0; c < copies; c++) {
        const dropPhone = rng() < 0.25;
        const dropEmail = !dropPhone && rng() < 0.25;
        pushRow(
          {
            external_id: `crm-${externalSeq++}`,
            full_name: dirtyName(rng, person),
            phone: dropPhone ? "" : formatPhoneDirty(rng, person.phoneBody),
            email: dropEmail ? "" : dirtyEmail(rng, person.email),
            birth_date: dirtyBirth(rng, person.birth),
            city: dirtyCity(rng, person.city),
          },
          person.id,
        );
      }
    }
  }

  // ~3% garbage rows (unique truth ids so they are not true duplicates)
  const garbageCount = Math.floor(TARGET_BASE * 0.03);
  for (let i = 0; i < garbageCount; i++) {
    const truthId = 100_000 + i;
    const kind = Math.floor(rng() * 4);
    pushRow(
      {
        external_id: `crm-${externalSeq++}`,
        full_name: kind === 0 ? "" : pick(rng, ["???"]),
        phone: kind === 1 ? "12345" : "",
        email: kind === 2 ? "not-an-email" : "",
        birth_date: kind === 3 ? "31.02.1990" : "99.99.9999",
        city: "",
      },
      truthId,
    );
  }

  // shuffle
  for (let i = rows.length - 1; i > 0; i--) {
    const j = Math.floor(rng() * (i + 1));
    [rows[i], rows[j]] = [rows[j], rows[i]];
  }

  const header = ["external_id", "full_name", "phone", "email", "birth_date", "city"].join(";");
  const csvLines = [header, ...rows.map((r) => rowToLine(r))];

  // truth: groups with 2+ members by truth_id, using 1-based data row numbers
  const groups = new Map();
  rows.forEach((r, idx) => {
    const rowNumber = idx + 1;
    if (!groups.has(r.truth_id)) groups.set(r.truth_id, []);
    groups.get(r.truth_id).push(rowNumber);
  });

  const truthLines = ["truth_id;row_numbers"];
  for (const [truthId, rowNumbers] of [...groups.entries()].sort((a, b) => a[0] - b[0])) {
    if (rowNumbers.length < 2) continue;
    truthLines.push(`${truthId};${rowNumbers.join(",")}`);
  }

  mkdirSync(samplesDir, { recursive: true });
  writeFileSync(join(samplesDir, "customers.csv"), csvLines.join("\n") + "\n", "utf8");
  writeFileSync(join(samplesDir, "customers.truth.csv"), truthLines.join("\n") + "\n", "utf8");

  console.log(`Wrote ${rows.length} rows to samples/customers.csv`);
  console.log(`Wrote ${truthLines.length - 1} duplicate truth groups to samples/customers.truth.csv`);
}

main();
