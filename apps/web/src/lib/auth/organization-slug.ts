const turkishCharacters: Record<string, string> = {
  Ç: "c",
  ç: "c",
  Ğ: "g",
  ğ: "g",
  I: "i",
  İ: "i",
  ı: "i",
  Ö: "o",
  ö: "o",
  Ş: "s",
  ş: "s",
  Ü: "u",
  ü: "u",
};

export function organizationSlugFromName(name: string) {
  return Array.from(name)
    .map((character) => turkishCharacters[character] ?? character)
    .join("")
    .normalize("NFKD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 100)
    .replace(/-+$/g, "");
}
