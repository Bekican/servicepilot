export type ActionState = {
  error?: string;
  supportCode?: string;
  redirectTo?: string;
  values?: Record<string, string>;
  fieldErrors?: Record<string, string>;
};

export const initialActionState: ActionState = {};

export function formValues(formData: FormData) {
  return Object.fromEntries(
    Array.from(formData.entries(), ([key, value]) => [key, String(value)]),
  );
}
