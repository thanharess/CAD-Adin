dimdel : dialog {
  label = "Xóa Dimension";
  width = 40;
  : column {
    : row {
      : text {
        label = "Chọn chức năng:";
        alignment = centered;
      }
    }
    : radio_row {
      key = "options";
      : radio_button { key = "selected_dim"; label = "1. Xóa DIMENSION được chọn"; }
      : radio_button { key = "dim_by_layer"; label = "2. Xóa DIMENSION theo layer"; }
      : radio_button { key = "dim_mleader"; label = "3. Xóa LEADER NOTE (MLEADER)"; }
      : radio_button { key = "dim_by_layer_select"; label = "4. Xóa DIMENSION theo layer và vùng chọn"; }
      : radio_button { key = "all_dim"; label = "5. Xóa tất cả DIMENSION"; }
    }
    : row {
      : column {
        fixed_width = true;
        alignment = left;
        : text { label = "Chọn Layer (cho chức năng 2 và 4):"; }
      }
      : column {
        : popup_list {
          key = "layerlist";
          value = "0";
          width = 25;
        }
      }
    }
  }
  : row {
    spacer_1;
    ok_cancel;
  }
}