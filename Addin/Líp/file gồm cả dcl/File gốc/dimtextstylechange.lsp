; dimtextstyle_v5_memory.lsp
; AutoCAD Mechanical 2020+
; Có chức năng nhớ lựa chọn DimStyle & Layer cũ.

(vl-load-com)

(defun get-collection-names (coll / names i count item)
  (setq names '())
  (setq count (vla-get-Count coll))
  (setq i 0)
  (while (< i count)
    (setq item (vla-Item coll i))
    (setq names (cons (vla-get-Name item) names))
    (setq i (1+ i))
  )
  (reverse names)
)

(defun write-dcl-file (/ dcl-str f temp-path)
  (setq temp-path (strcat (getenv "TEMP") "\\dimtextstyle_v5_memory.dcl"))
  (setq dcl-str
"dimbasestyle : dialog {
  label = \"DIMTEXTSTYLE v5 (Memory)\";
  : column {
    : text { label = \"Chọn chế độ thao tác:\"; alignment = centered; }
    : radio_column { key = \"mode\";
      : radio_button { key = \"mode1\"; label = \"1. Thay đổi TẤT CẢ DIM trong bản vẽ\"; value = \"mode1\"; }
      : radio_button { key = \"mode2\"; label = \"2. Thay đổi DIM theo vùng chọn\"; value = \"mode2\"; }
      : radio_button { key = \"mode4\"; label = \"3. Thay Base DIM Text Style theo style nguồn\"; value = \"mode4\"; }
    }

    : boxed_column { label = \"Tùy chọn chế độ 2\"; 
      : radio_column { key = \"layeropt_sel\";
        : radio_button { key = \"sel1\"; label = \"1. Đổi tất cả DIM trong vùng chọn sang style đích\"; value = \"sel1\"; }
        : radio_button { key = \"sel2\"; label = \"2. Chỉ đổi DIM có layer trùng với layer nguồn\"; value = \"sel2\"; }
      }
    }

    : row { : text { label = \"Style nguồn:\"; alignment = left; }
      : popup_list { key = \"sourcelist\"; width = 30; }
    }

    : row { : text { label = \"Style đích:\"; alignment = left; }
      : popup_list { key = \"targetlist\"; width = 40; }
    }

    : row { : text { label = \"Layer nguồn:\"; alignment = left; }
      : popup_list { key = \"layerlist\"; width = 30; }
    }

  }
  ok_cancel;
}"
  )
  (if (setq f (open temp-path "w"))
    (progn (write-line dcl-str f) (close f) temp-path)
    (progn (alert "Không thể tạo file DCL!") nil))
)

(defun get-dimstyle-from-index (index-str list-styles / idx)
  (if (and index-str (setq idx (atoi index-str)) (< idx (length list-styles)))
    (nth idx list-styles)
    (if list-styles (car list-styles) nil)
  )
)

(defun get-layer-from-index (index-str list-layers / idx)
  (if (and index-str (setq idx (atoi index-str)) (< idx (length list-layers)))
    (nth idx list-layers)
    (if list-layers (car list-layers) nil)
  )
)

(defun update-dim-style (dim-obj new-style)
  (if (and new-style (vlax-property-available-p dim-obj 'StyleName))
    (vla-put-StyleName dim-obj new-style)
  )
)

(defun get-dim-layer (dim-obj)
  (vla-get-Layer dim-obj)
)

(defun find-index (name list)
  (setq idx 0)
  (while (and list (/= (car list) name))
    (setq idx (1+ idx) list (cdr list))
  )
  (if (< idx (length list)) idx 0)
)

(defun c:DIMTEXTSTYLE (/ dcl_id dcl_file mode sourcestyle targetstyle layeropt_sel sourcelayer
                       dimstyles layers source-index target-index layer-index ss i e dim-obj
                       current-layer changed-count doc result lastSource lastTarget lastLayer)

  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (setq dimstyles (get-collection-names (vla-get-DimStyles doc)))
  (setq layers (get-collection-names (vla-get-Layers doc)))

  ;; đọc giá trị nhớ trước đó
  (setq lastSource (getenv "DIMTXT_SRC"))
  (setq lastTarget (getenv "DIMTXT_TGT"))
  (setq lastLayer (getenv "DIMTXT_LYR"))

  (setq dcl_file (write-dcl-file))
  (if (not dcl_file) (exit))

(setq dcl_file (write-dcl-file))
(if (or (not dcl_file) (not (findfile dcl_file)))
  (progn
    (alert "Không thể tạo hoặc tìm thấy file DCL. Kiểm tra quyền thư mục TEMP.")
    (exit)
  )
)


  (setq dcl_id (load_dialog dcl_file))
  (if (not (new_dialog "dimbasestyle" dcl_id)) (exit))

  ;; mặc định
  (set_tile "mode2" "1")
  (set_tile "sel1" "1")

  ;; fill lists
  (start_list "sourcelist" 3) (mapcar 'add_list dimstyles) (end_list)
  (start_list "targetlist" 3) (mapcar 'add_list dimstyles) (end_list)
  (start_list "layerlist" 3) (mapcar 'add_list layers) (end_list)

  ;; set mặc định nếu có nhớ
  (if lastSource (set_tile "sourcelist" (itoa (find-index lastSource dimstyles))))
  (if lastTarget (set_tile "targetlist" (itoa (find-index lastTarget dimstyles))))
  (if lastLayer  (set_tile "layerlist"  (itoa (find-index lastLayer layers))))

  ;; actions
  (action_tile "accept"
    "(progn
       (setq source-index (get_tile \"sourcelist\"))
       (setq target-index (get_tile \"targetlist\"))
       (setq layer-index (get_tile \"layerlist\"))
       (setq mode (get_tile \"mode\"))
       (setq layeropt_sel (get_tile \"layeropt_sel\"))
       (if (or (null mode) (= mode \"\")) (setq mode \"mode1\"))
       (if (or (null layeropt_sel) (= layeropt_sel \"\")) (setq layeropt_sel \"sel1\"))
       (done_dialog 1)
     )")
  (action_tile "cancel" "(done_dialog 0)")

  (setq result (start_dialog))
  (unload_dialog dcl_id)

  ;; lấy style/layer theo index
  (setq sourcestyle (get-dimstyle-from-index source-index dimstyles))
  (setq targetstyle (get-dimstyle-from-index target-index dimstyles))
  (setq sourcelayer (get-layer-from-index layer-index layers))

  ;; lưu lại lựa chọn
  (if sourcestyle (setenv "DIMTXT_SRC" sourcestyle))
  (if targetstyle (setenv "DIMTXT_TGT" targetstyle))
  (if sourcelayer (setenv "DIMTXT_LYR" sourcelayer))

  ;; xử lý mode
  (cond
    ;; ===== MODE 1 =====
    ((and (= result 1) (= mode "mode1"))
     (princ "\n[Chế độ 1] Thay tất cả DIM sang style đích...")
     (setq ss (ssget "X" '((0 . "DIMENSION"))))
     (if ss
       (progn
         (setq changed-count 0 i (sslength ss))
         (while (> i 0)
           (setq i (1- i) e (ssname ss i))
           (setq dim-obj (vlax-ename->vla-object e))
           (update-dim-style dim-obj targetstyle)
           (setq changed-count (1+ changed-count))
         )
         (princ (strcat "\nĐã đổi " (itoa changed-count) " DIM sang style: " targetstyle))
       )
       (princ "\nKhông có DIM nào trong bản vẽ.")
     )
    )

    ;; ===== MODE 2 =====
    ((and (= result 1) (= mode "mode2"))
     (princ "\n[Chế độ 2] Chọn vùng chứa DIM cần đổi...")
     (setq ss (ssget '((0 . "DIMENSION"))))
     (if ss
       (progn
         (setq changed-count 0 i (sslength ss))
         (while (> i 0)
           (setq i (1- i) e (ssname ss i))
           (setq dim-obj (vlax-ename->vla-object e))
           (setq current-layer (get-dim-layer dim-obj))
           (cond
             ((= layeropt_sel "sel1")
              (update-dim-style dim-obj targetstyle)
              (setq changed-count (1+ changed-count))
             )
             ((= layeropt_sel "sel2")
              (if (and sourcelayer (= current-layer sourcelayer))
                (progn
                  (update-dim-style dim-obj targetstyle)
                  (setq changed-count (1+ changed-count))
                )
              )
             )
           )
         )
         (princ (strcat "\nĐã đổi " (itoa changed-count) " DIM theo tùy chọn."))
       )
       (princ "\nKhông chọn được DIM nào.")
     )
    )

    ;; ===== MODE 3 =====
    ((and (= result 1) (= mode "mode4"))
     (princ "\n[Chế độ 3] Cập nhật Base DIM Text Style theo style nguồn...")
     (if (and sourcestyle (/= sourcestyle ""))
       (progn
         (if (tblsearch "dimstyle" sourcestyle)
           (progn
             (command "_.-DIMSTYLE" "_R" sourcestyle "")
             (princ (strcat "\nBase DIM Text Style đã đặt theo: " sourcestyle))
           )
           (alert (strcat "Không tìm thấy Dimension Style: " sourcestyle))
         )
       )
       (alert "Không có style nguồn hợp lệ!")
     )
    )

    (t (princ "\nHủy hoặc không có thao tác nào được thực hiện."))
  )

  (princ)
)
