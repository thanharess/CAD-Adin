dimbasestyle : dialog {
  label = "DIMTEXTSTYLE v5 (Memory)";
  : column {
    : text { label = "Chọn chế độ thao tác:"; alignment = centered; }
    : radio_column { key = "mode";
      : radio_button { key = "mode1"; label = "1. Thay đổi TẤT CẢ DIM trong bản vẽ"; value = "mode1"; }
      : radio_button { key = "mode2"; label = "2. Thay đổi DIM theo vùng chọn"; value = "mode2"; }
      : radio_button { key = "mode4"; label = "3. Thay Base DIM Text Style theo style nguồn"; value = "mode4"; }
    }

    : boxed_column { label = "Tùy chọn chế độ 2"; 
      : radio_column { key = "layeropt_sel";
        : radio_button { key = "sel1"; label = "1. Đổi tất cả DIM trong vùng chọn sang style đích"; value = "sel1"; }
        : radio_button { key = "sel2"; label = "2. Chỉ đổi DIM có layer trùng với layer nguồn"; value = "sel2"; }
      }
    }

    : row { : text { label = "Style nguồn:"; alignment = left; }
      : popup_list { key = "sourcelist"; width = 30; }
    }

    : row { : text { label = "Style đích:"; alignment = left; }
      : popup_list { key = "targetlist"; width = 40; }
    }

    : row { : text { label = "Layer nguồn:"; alignment = left; }
      : popup_list { key = "layerlist"; width = 30; }
    }

  }
  ok_cancel;
}