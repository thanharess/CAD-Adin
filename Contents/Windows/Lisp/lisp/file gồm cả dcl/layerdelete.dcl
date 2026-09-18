layerdel : dialog {
  label = "Xóa Layer";
  width = 45;
  : column {
    : text { label = "Chọn chức năng:"; alignment = centered; }
    : radio_column {
      key = "options";
      : radio_button { key = "dim_by_layer_select"; label = "1. Xóa Line theo layer trong vùng chọn"; value = "1"; }
      : radio_button { key = "dim_by_layer"; label = "2. Xóa tất cả Line trên layer"; }
      : radio_button { key = "all_dim"; label = "3. Phá khối tất cả layer không dùng"; }
    }
    : row {
      : text { label = "Chọn Layer:"; alignment = left; }
      : popup_list { key = "layerlist"; width = 25; }
      : button { key = "pick_layer"; label = "Chọn Entity"; width = 12; }
    }
  }
  ok_cancel;
}