import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { SupportCode } from "@/components/shared/support-code";

describe("SupportCode", () => {
  it("copies a validated correlation ID", () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, "clipboard", {
      configurable: true,
      value: { writeText },
    });

    render(<SupportCode value="web-request_1234" />);
    fireEvent.click(
      screen.getByRole("button", {
        name: "Destek kodunu kopyala",
      }),
    );

    expect(writeText).toHaveBeenCalledWith("web-request_1234");
  });

  it("does not render an invalid value", () => {
    const { container } = render(<SupportCode value="unsafe support value" />);

    expect(container.innerHTML).toBe("");
  });
});
