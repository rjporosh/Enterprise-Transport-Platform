/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{html,ts}"],
  theme: {
    extend: {
      colors: {
        // "Night coach" palette: deep indigo (the road at dusk) with a
        // saffron accent (the ticket-stub color you'd actually see printed
        // on a Dhaka-Chattogram coach ticket) — chosen for this domain
        // rather than a generic SaaS blue.
        ink: {
          950: "#0B1220",
          900: "#121A2E",
          800: "#1B2740",
          700: "#28365A"
        },
        saffron: {
          500: "#E8A33D",
          600: "#D08A22"
        }
      },
      fontFamily: {
        display: ["'Fraunces'", "serif"],
        body: ["'Inter'", "sans-serif"]
      }
    }
  },
  plugins: []
};
