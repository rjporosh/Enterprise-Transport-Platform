/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        // Same "night coach" family as the customer app for brand
        // continuity, but the admin surface leans lighter/denser — this is
        // an operations console, not a marketing storefront.
        ink: { 950: "#0B1220", 900: "#121A2E", 800: "#1B2740", 700: "#28365A" },
        saffron: { 500: "#E8A33D", 600: "#D08A22" },
        slate: { 50: "#F5F7FA" }
      },
      fontFamily: {
        display: ["'Fraunces'", "serif"],
        body: ["'Inter'", "sans-serif"]
      }
    }
  },
  plugins: []
};
