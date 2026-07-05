/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{html,ts}"],
  important: true,
  theme: {
    extend: {
      colors: {
        // פלטת Fintech Midnight Premium
        'bg-primary':   '#0F172A',   // Midnight Navy
        'bg-surface':   '#1E293B',   // Slate Blue
        'bg-elevated':  '#263348',
        'accent-blue':  '#3B82F6',   // Steel Blue (CTA ראשי)
        'accent-ice':   '#60A5FA',   // Ice Blue
        'success':      '#10B981',   // Emerald Green (אנומליה)

        // תאימות לקוד קיים
        'ai-neon':      '#00f2ff',
        'ai-purple':    '#8c54ff',
        'ai-dark':      '#0F172A',
        'ai-deeper':    '#0B111E',
        'ai-gold':      '#ffd700',
      },
      boxShadow: {
        'neon-blue':   '0 0 20px rgba(59, 130, 246, 0.4)',
        'neon-cyan':   '0 0 20px rgba(0, 242, 255, 0.3)',
        'neon-green':  '0 0 20px rgba(16, 185, 129, 0.4)',
      }
    }
  },
  plugins: []
};