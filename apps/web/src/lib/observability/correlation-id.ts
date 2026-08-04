export const correlationIdHeader = "x-correlation-id";

const correlationIdPattern = /^[A-Za-z0-9][A-Za-z0-9._-]{7,63}$/;

export function isValidCorrelationId(
  value: string | null | undefined,
): value is string {
  return (
    value !== null && value !== undefined && correlationIdPattern.test(value)
  );
}

export function resolveCorrelationId(value: string | null | undefined) {
  return isValidCorrelationId(value)
    ? value
    : crypto.randomUUID().replaceAll("-", "");
}
