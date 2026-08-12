/**
 * Menu Optics Landing Page Interactions, Theme Toggle & Motion Scripts
 */
document.addEventListener('DOMContentLoaded', function () {
    // 0. Theme Switcher Logic (Sun & Moon)
    const themeToggleBtn = document.getElementById('themeToggleBtn');
    const themeIcon = document.getElementById('themeIcon');
    const htmlElement = document.documentElement;

    // Check saved theme or default to dark
    const savedTheme = localStorage.getItem('menuoptics_theme') || 'dark';
    applyTheme(savedTheme);

    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', function () {
            const currentTheme = htmlElement.getAttribute('data-theme') || 'dark';
            const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
            applyTheme(newTheme);
            localStorage.setItem('menuoptics_theme', newTheme);
        });
    }

    function applyTheme(theme) {
        htmlElement.setAttribute('data-theme', theme);
        if (themeIcon) {
            if (theme === 'light') {
                themeIcon.classList.remove('fa-sun');
                themeIcon.classList.add('fa-moon');
                if (themeToggleBtn) {
                    themeToggleBtn.setAttribute('title', 'Switch to Dark Mode');
                    themeToggleBtn.setAttribute('aria-label', 'Switch to Dark Mode');
                }
            } else {
                themeIcon.classList.remove('fa-moon');
                themeIcon.classList.add('fa-sun');
                if (themeToggleBtn) {
                    themeToggleBtn.setAttribute('title', 'Switch to Light Mode');
                    themeToggleBtn.setAttribute('aria-label', 'Switch to Light Mode');
                }
            }
        }
    }

    // 1. Sticky Header scroll effect
    const header = document.querySelector('.landing-header');
    window.addEventListener('scroll', function () {
        if (window.scrollY > 40) {
            header.classList.add('scrolled');
        } else {
            header.classList.remove('scrolled');
        }
    });

    // 2. Mobile Nav Menu Toggle
    const mobileToggle = document.getElementById('mobileNavToggle');
    const navMenu = document.getElementById('navMenu');
    if (mobileToggle && navMenu) {
        mobileToggle.addEventListener('click', function () {
            navMenu.classList.toggle('active');
            const icon = mobileToggle.querySelector('i');
            if (icon) {
                if (navMenu.classList.contains('active')) {
                    icon.classList.remove('fa-bars');
                    icon.classList.add('fa-times');
                } else {
                    icon.classList.remove('fa-times');
                    icon.classList.add('fa-bars');
                }
            }
        });
    }

    // 3. Scroll Reveal Animations via IntersectionObserver
    const reveals = document.querySelectorAll('.reveal');
    if ('IntersectionObserver' in window) {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('active');
                }
            });
        }, {
            threshold: 0.15
        });

        reveals.forEach(reveal => observer.observe(reveal));
    } else {
        // Fallback for older browsers
        reveals.forEach(reveal => reveal.classList.add('active'));
    }

    // 4. Smooth Anchor Scrolling
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const targetId = this.getAttribute('href');
            if (targetId === '#' || !targetId.startsWith('#')) return;
            const targetElement = document.querySelector(targetId);
            if (targetElement) {
                e.preventDefault();
                const offset = 90; // Header offset
                const bodyRect = document.body.getBoundingClientRect().top;
                const elementRect = targetElement.getBoundingClientRect().top;
                const elementPosition = elementRect - bodyRect;
                const offsetPosition = elementPosition - offset;

                window.scrollTo({
                    top: offsetPosition,
                    behavior: 'smooth'
                });

                // Close mobile menu if open
                if (navMenu && navMenu.classList.contains('active')) {
                    navMenu.classList.remove('active');
                    const icon = mobileToggle.querySelector('i');
                    if (icon) {
                        icon.classList.remove('fa-times');
                        icon.classList.add('fa-bars');
                    }
                }
            }
        });
    });

    // 5. Stat Counter Animation
    const counters = document.querySelectorAll('.stat-counter-number');
    if (counters.length > 0 && 'IntersectionObserver' in window) {
        const counterObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const counter = entry.target;
                    const target = +counter.getAttribute('data-target');
                    const speed = 200;
                    const increment = target / speed;

                    const updateCount = () => {
                        const count = +counter.innerText.replace(/[^0-9.]/g, '');
                        if (count < target) {
                            counter.innerText = Math.ceil(count + increment) + (counter.getAttribute('data-suffix') || '');
                            setTimeout(updateCount, 15);
                        } else {
                            counter.innerText = target + (counter.getAttribute('data-suffix') || '');
                        }
                    };
                    updateCount();
                    counterObserver.unobserve(counter);
                }
            });
        }, { threshold: 0.5 });

        counters.forEach(c => counterObserver.observe(c));
    }
});
