export function formatDateTime(
  value: string,
  timeZone: string,
  options: Intl.DateTimeFormatOptions,
) {
  return new Intl.DateTimeFormat("tr-TR", {
    timeZone,
    ...options,
  }).format(new Date(value));
}

export function formatTime(value: string, timeZone: string) {
  return formatDateTime(value, timeZone, {
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function formatDate(value: string, timeZone: string) {
  return formatDateTime(value, timeZone, {
    day: "2-digit",
    month: "long",
    year: "numeric",
  });
}

export function dateKey(value: string, timeZone: string) {
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(new Date(value));
  const valueOf = (type: Intl.DateTimeFormatPartTypes) =>
    parts.find((part) => part.type === type)?.value ?? "";

  return `${valueOf("year")}-${valueOf("month")}-${valueOf("day")}`;
}

export function zonedLocalDateTimeToIso(value: string, timeZone: string) {
  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/.exec(value);
  if (!match) {
    throw new Error("Geçerli bir tarih ve saat seçin.");
  }

  const desired = {
    year: Number(match[1]),
    month: Number(match[2]),
    day: Number(match[3]),
    hour: Number(match[4]),
    minute: Number(match[5]),
  };
  const desiredAsUtc = Date.UTC(
    desired.year,
    desired.month - 1,
    desired.day,
    desired.hour,
    desired.minute,
  );
  const formatter = new Intl.DateTimeFormat("en-CA", {
    timeZone,
    hourCycle: "h23",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });

  let instant = desiredAsUtc;
  for (let index = 0; index < 2; index += 1) {
    const parts = formatter.formatToParts(new Date(instant));
    const part = (type: Intl.DateTimeFormatPartTypes) =>
      Number(parts.find((item) => item.type === type)?.value);
    const observedAsUtc = Date.UTC(
      part("year"),
      part("month") - 1,
      part("day"),
      part("hour"),
      part("minute"),
    );
    instant += desiredAsUtc - observedAsUtc;
  }

  return new Date(instant).toISOString();
}
