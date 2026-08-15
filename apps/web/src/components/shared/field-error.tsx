export function FieldError({ id, message }: { id: string; message?: string }) {
  if (!message) return null;

  return (
    <p className="text-destructive text-xs" id={id} role="alert">
      {message}
    </p>
  );
}
