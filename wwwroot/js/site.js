// Chart.js Initialization
document.addEventListener("DOMContentLoaded", function() {
    var ctx = document.getElementById("salesChart");
    if (ctx) {
        // Fetch real data from API
        fetch('/Customers/GetSalesData')
            .then(response => response.json())
            .then(data => {
                var labels = data.map(item => item.date);
                var values = data.map(item => item.totalAmount);

                var myLineChart = new Chart(ctx, {
                    type: 'line',
                    data: {
                        labels: labels,
                        datasets: [{
                            label: "Kazanç",
                            lineTension: 0.3,
                            backgroundColor: "rgba(78, 115, 223, 0.05)",
                            borderColor: "rgba(78, 115, 223, 1)",
                            pointRadius: 3,
                            pointBackgroundColor: "rgba(78, 115, 223, 1)",
                            pointBorderColor: "rgba(78, 115, 223, 1)",
                            pointHoverRadius: 3,
                            pointHoverBackgroundColor: "rgba(78, 115, 223, 1)",
                            pointHoverBorderColor: "rgba(78, 115, 223, 1)",
                            pointHitRadius: 10,
                            pointBorderWidth: 2,
                            data: values,
                        }],
                    },
                    options: {
                        maintainAspectRatio: false,
                        layout: {
                            padding: {
                                left: 10,
                                right: 25,
                                top: 25,
                                bottom: 0
                            }
                        },
                        scales: {
                            x: {
                                grid: {
                                    display: false,
                                    drawBorder: false
                                },
                                ticks: {
                                    maxTicksLimit: 7
                                }
                            },
                            y: {
                                ticks: {
                                    maxTicksLimit: 5,
                                    padding: 10,
                                    // Include a dollar sign in the ticks
                                    callback: function(value, index, values) {
                                        return '₺' + value;
                                    }
                                },
                                grid: {
                                    color: "rgb(234, 236, 244)",
                                    zeroLineColor: "rgb(234, 236, 244)",
                                    drawBorder: false,
                                    borderDash: [2],
                                    zeroLineBorderDash: [2]
                                }
                            },
                        },
                        plugins: {
                            legend: {
                                display: false
                            },
                            tooltip: {
                                backgroundColor: "rgb(255,255,255)",
                                bodyColor: "#858796",
                                titleMarginBottom: 10,
                                titleColor: '#6e707e',
                                titleFontSize: 14,
                                borderColor: '#dddfeb',
                                borderWidth: 1,
                                xPadding: 15,
                                yPadding: 15,
                                displayColors: false,
                                intersect: false,
                                mode: 'index',
                                caretPadding: 10,
                                callbacks: {
                                    label: function(tooltipItem, chart) {
                                        var datasetLabel = tooltipItem.dataset.label || '';
                                        return datasetLabel + ': ₺' + tooltipItem.raw;
                                    }
                                }
                            }
                        }
                    }
                });
            })
            .catch(error => console.error('Error fetching sales data:', error));
    }
});

// Search functionality removed from here to avoid conflict with server-side search in Index.cshtml
// $(document).ready(function() {
//   $("#dataTableSearch").on("keyup", function() {
//     var value = $(this).val().toLowerCase();
//     $("#dataTable tbody tr").filter(function() {
//       $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1)
//     });
//   });
// });

  // Toggle the side navigation
  $(document).ready(function() {
    $("#sidebarToggle, #sidebarToggleTop").on('click', function(e) {
      e.preventDefault();
      e.stopPropagation();
      $("body").toggleClass("sidebar-toggled");
      $(".sidebar").toggleClass("toggled");
      if ($(".sidebar").hasClass("toggled")) {
        $('.sidebar .collapse').collapse('hide');
      };
      
      // Icon değişimini sağla
      var icon = $("#sidebarToggle i");
      if ($(".sidebar").hasClass("toggled")) {
          icon.removeClass("fa-chevron-left").addClass("fa-chevron-right");
      } else {
          icon.removeClass("fa-chevron-right").addClass("fa-chevron-left");
      }
    });
  });
