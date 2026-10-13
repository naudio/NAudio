// NAudio DocFX customizations.

// The navbar logo is an <img>, so it can't follow the theme by itself. Swap in
// the dark-background variant whenever the theme switcher (or the "auto"
// setting following the OS) puts the page in dark mode.
export default {
  start: () => {
    const logo = document.getElementById("logo");
    if (!logo) return;
    const light = logo.getAttribute("src");
    const dark = light.replace(/naudio-icon\.svg$/, "naudio-icon-dark.svg");
    const root = document.documentElement;
    const sync = () => logo.setAttribute("src", root.getAttribute("data-bs-theme") === "dark" ? dark : light);
    sync();
    new MutationObserver(sync).observe(root, { attributes: true, attributeFilter: ["data-bs-theme"] });
  },
};
