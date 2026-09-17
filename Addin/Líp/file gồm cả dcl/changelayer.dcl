changelayer : dialog {
  label = "Chuyển Layer";
  width = 50;
  : column {
    : text { label = "Chọn layer nguồn và đích:"; alignment = centered; }
    : radio_column {
      key = "mode";
      : radio_button { key = "all_mode"; label = "Chuyển tất cả trên layer nguồn"; value = "1"; }
      : radio_button { key = "select_mode"; label = "Chuyển entity được chọn trong vùng"; }
    }
    : row {
      : column {
        : text { label = "Layer nguồn:"; alignment = left; }
        : row {
          : popup_list { key = "sourcelist"; width = 20; }
          : button { key = "pick_source"; label = "Chọn Entity"; width = 12; }
        }
      }
      : column {
        : text { label = "Layer đích:"; alignment = left; }
        : popup_list { key = "targetlist"; width = 20; }
      }
    }
  }
  ok_cancel;
}