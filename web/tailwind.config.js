/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{js,jsx,ts,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // Vistora brand palette
        primary: {
          50: '#e8f6fb',
          100: '#c6e9f5',
          200: '#9bd9ed',
          300: '#66c4e2',
          400: '#39b3d7',
          500: '#20a7c9', // primary
          600: '#1a89a6',
          700: '#176c83',
          800: '#14506a', // dark
          900: '#0f3a4d',
        },
        accent: {
          50: '#f5f5f5',
          100: '#ededed',
          200: '#d9d9d9',
          300: '#bfbfbf',
          400: '#999999',
          500: '#666666',
          600: '#484848', // grayscale dark
          700: '#323232',
          800: '#1f1f1f',
          900: '#111111',
        },
        success: {
          50: '#eef9f2',
          100: '#d3f0de',
          200: '#a7e0bd',
          300: '#7bd09c',
          400: '#5ac189', // success
          500: '#44b078',
          600: '#359160',
          700: '#2a7049',
          800: '#1f5036',
          900: '#143322',
        },
        warning: {
          50: '#fff9e0',
          100: '#fdefb3',
          200: '#fcdf66',
          300: '#fcd226',
          400: '#fcc700', // warning
          500: '#e0b000',
          600: '#b38c00',
          700: '#866900',
          800: '#594600',
          900: '#332800',
        },
        error: {
          50: '#fdecee',
          100: '#fac8cf',
          200: '#f49ba8',
          300: '#ed6d80',
          400: '#e04355', // error
          500: '#cc3848',
          600: '#a82d3a',
          700: '#82232d',
          800: '#5c1920',
          900: '#3a1015',
        },
        info: {
          50: '#eef6ff',
          100: '#d6e9ff',
          200: '#aed3ff',
          300: '#85bdff',
          400: '#66bcfe', // info
          500: '#3a9bf0',
          600: '#2778c4',
          700: '#1d5a93',
          800: '#143d63',
          900: '#0c2438',
        },
      },
      fontFamily: {
        sans: ['Inter', '"Helvetica Neue"', 'Helvetica', 'Arial', 'system-ui', 'sans-serif'],
        mono: ['"Fira Code"', 'JetBrains Mono', 'Menlo', 'Monaco', 'monospace'],
      },
      boxShadow: {
        card: '0 0 6px 1px rgba(0, 0, 0, 0.08)',
        'card-hover': '0 0 12px 2px rgba(0, 0, 0, 0.12)',
      },
    },
  },
  plugins: [],
}
