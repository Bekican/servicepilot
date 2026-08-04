import type { components } from "@/lib/api/generated/schema";

export type ApiSchema = components["schemas"];
export type Session = ApiSchema["CurrentUserResponse"];
export type Customer = ApiSchema["CustomerResponse"];
export type CustomerAddress = ApiSchema["CustomerAddressResponse"];
export type Service = ApiSchema["ServiceResponse"];
export type Appointment = ApiSchema["AppointmentResponse"];
export type Reminder = ApiSchema["ReminderResponse"];
export type User = ApiSchema["UserResponse"];
export type Invitation = ApiSchema["UserInvitationResponse"];
export type Technician = ApiSchema["TechnicianResponse"];
export type DashboardSummary = ApiSchema["DashboardSummaryResponse"];
export type ProblemDetails = ApiSchema["ProblemDetails"] & {
  code?: string;
  traceId?: string;
  correlationId?: string;
  errors?: Record<string, string[]>;
};
