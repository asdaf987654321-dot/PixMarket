// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.store-cart-form').forEach(function (form) {
        var value = form.querySelector('.qty-value');
        var hidden = form.querySelector('.qty-hidden');
        var less = form.querySelector('.btn-qty-less');
        var more = form.querySelector('.btn-qty-more');
        var quantity = form.querySelector('.quantity');
        var submit = form.querySelector('.store-cart');

        var stock = quantity ? parseInt(quantity.dataset.stock, 10) : 0;
        if (isNaN(stock) || stock < 0) {
            stock = 0;
        }

        if (stock <= 0 && submit) {
            submit.disabled = true;
            submit.style.opacity = '0.55';
            submit.style.cursor = 'not-allowed';
        }

        if (less) {
            less.addEventListener('click', function () {
                var n = parseInt(value.textContent, 10);
                if (n > 1) {
                    n--;
                    value.textContent = n;
                    hidden.value = n;
                }
            });
        }

        if (more && stock > 0) {
            more.addEventListener('click', function () {
                var n = parseInt(value.textContent, 10);
                if (n < stock) {
                    n++;
                    value.textContent = n;
                    hidden.value = n;
                }
            });
        }
    });

    document.querySelectorAll('.fake-slider').forEach(function (slider) {
        var minInput = slider.querySelector('.slider-input.min');
        var maxInput = slider.querySelector('.slider-input.max');
        var filterSection = slider.closest('.filter-section');
        var minHidden = filterSection ? filterSection.querySelector('.precio-min-hidden') : null;
        var maxHidden = filterSection ? filterSection.querySelector('.precio-max-hidden') : null;
        var labelMin = filterSection ? filterSection.querySelector('.valor-min') : null;
        var labelMax = filterSection ? filterSection.querySelector('.valor-max') : null;
        var maxVal = parseInt(slider.dataset.max, 10) || 1000;

        if (!minInput || !maxInput) {
            return;
        }

        // Si no había filtro previo, los hidden deben quedar vacíos
        // hasta que el usuario toque el slider.
        var activoMin = slider.dataset.minVal || '';
        var activoMax = slider.dataset.maxVal || '';

        function formatLabel(val) {
            return 'Bs ' + Number(val).toLocaleString('en-US');
        }

        function actualizarEtiquetas() {
            var min = parseInt(minInput.value, 10);
            var max = parseInt(maxInput.value, 10);

            if (labelMin) {
                labelMin.textContent = formatLabel(min);
            }

            if (labelMax) {
                labelMax.textContent = max >= maxVal
                    ? 'Bs ' + Number(maxVal).toLocaleString('en-US') + '+'
                    : formatLabel(max);
            }

            if (activoMin !== '') {
                minHidden.value = min;
            }

            if (activoMax !== '') {
                maxHidden.value = max;
            }
        }

        minInput.addEventListener('input', function () {
            var min = parseInt(minInput.value, 10);
            var max = parseInt(maxInput.value, 10);
            if (min > max - 1) {
                min = max - 1;
                minInput.value = min;
            }
            minHidden.value = min;
            activoMin = String(min);
            actualizarEtiquetas();
        });

        maxInput.addEventListener('input', function () {
            var min = parseInt(minInput.value, 10);
            var max = parseInt(maxInput.value, 10);
            if (max < min + 1) {
                max = min + 1;
                maxInput.value = max;
            }
            maxHidden.value = max;
            activoMax = String(max);
            actualizarEtiquetas();
        });

        // Si hay un filtro previo, reflejarlo en las etiquetas
        if (activoMin !== '' || activoMax !== '') {
            actualizarEtiquetas();
        }
    });
});