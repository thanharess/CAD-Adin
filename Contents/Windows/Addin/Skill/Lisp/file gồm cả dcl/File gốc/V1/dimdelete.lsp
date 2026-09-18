; AutoLISP code for AutoCAD to delete Dimensions with embedded DCL in TEMP folder
; Updated: Added function 4: Delete DIMENSION by layer and selected area (combines layer filter + manual selection)
; Reordered: 1=old4 (selected), 2=old2 (by layer), 3=old3 (MLEADER), 4=new (by layer+select), 5=old1 (all)
; Load this LISP with (load "dimdelete.lsp") or APPLOAD

; Embedded DCL content as string
(defun write-dcl-file (/ dcl-str f temp-path)
  (setq temp-path (strcat (getenv "TEMP") "\\dimdelete.dcl"))
  (if (not temp-path) (setq temp-path "dimdelete.dcl"))  ; Fallback
  (setq dcl-str
    "dimdel : dialog {
      label = \"Xóa Dimension\";
      width = 40;
      : column {
        : row {
          : text {
            label = \"Chọn chức năng:\";
            alignment = centered;
          }
        }
        : radio_row {
          key = \"options\";
          : radio_button { key = \"selected_dim\"; label = \"1. Xóa DIMENSION được chọn\"; }
          : radio_button { key = \"dim_by_layer\"; label = \"2. Xóa DIMENSION theo layer\"; }
          : radio_button { key = \"dim_mleader\"; label = \"3. Xóa LEADER NOTE (MLEADER)\"; }
          : radio_button { key = \"dim_by_layer_select\"; label = \"4. Xóa DIMENSION theo layer và vùng chọn\"; }
          : radio_button { key = \"all_dim\"; label = \"5. Xóa tất cả DIMENSION\"; }
        }
        : row {
          : column {
            fixed_width = true;
            alignment = left;
            : text { label = \"Chọn Layer (cho chức năng 2 và 4):\"; }
          }
          : column {
            : popup_list {
              key = \"layerlist\";
              value = \"0\";
              width = 25;
            }
          }
        }
      }
      : row {
        spacer_1;
        ok_cancel;
      }
    }"
  )
  (if (setq f (open temp-path "w"))
    (progn
      (write-line dcl-str f)
      (close f)
      temp-path  ; Return path for load_dialog
    )
    (progn
      (alert (strcat "Không thể tạo file DCL tại " temp-path ". Kiểm tra quyền TEMP folder."))
      nil
    )
  )
)

; Main function
(defun c:dimdelete (/ dcl_id dcl-path layerlist layer lay ss i ent layname choice dcl-loaded result layindex)
  (setq choice nil layindex nil)  ; Initialize globals
  (setq dcl-loaded (write-dcl-file))
  (if (not dcl-loaded)
    (exit)
  )
  (setq dcl_id (load_dialog dcl-loaded))
  (if (not (new_dialog "dimdel" dcl_id))
    (progn
      (alert "Không load được dialog. Kiểm tra file DCL.")
      (vl-file-delete dcl-loaded)
      (exit)
    )
  )

  ; Populate layer dropdown correctly
  (setq layerlist '())
  (if (setq layer (tblnext "LAYER" T))
    (progn
      (while layer
        (setq layerlist (cons (cdr (assoc 2 layer)) layerlist))
        (setq layer (tblnext "LAYER"))
      )
      (setq layerlist (reverse layerlist))
    )
  )
  (if layerlist
    (progn
      (start_list "layerlist" 3)  ; 3 = no multiple select
      (foreach lay layerlist
        (add_list lay)
      )
      (end_list)
      (set_tile "layerlist" "0")
    )
    (progn
      (start_list "layerlist" 3)
      (add_list "Không có layer")
      (end_list)
      (set_tile "layerlist" "0")
    )
  )

  ; Select default radio button to new #1
  (set_tile "selected_dim" "1")

  ; Action for OK: Set choice and layindex before closing
  (action_tile "accept"
    "(progn
       (setq choice (get_tile \"options\"))
       (setq layindex (get_tile \"layerlist\"))
       (done_dialog 1)
     )"
  )
  (action_tile "cancel" "(done_dialog 0)")

  (setq result (start_dialog))
  (unload_dialog dcl_id)
  (vl-file-delete dcl-loaded)  ; Clean up

  ; Only execute if OK was pressed (result = 1)
  (if (= result 1)
    (progn
      ; Execute based on choice
      (cond
        ((= choice "selected_dim")
         (delete-selected-dim)
        )
        ((= choice "dim_by_layer")
         (if (and layindex layerlist (> (length layerlist) 0) (/= layindex ""))
           (progn
             (setq layname (nth (atoi layindex) layerlist))
             (if (and layname (/= layname "Không có layer"))
               (delete-dim-by-layer layname)
               (alert "Layer không hợp lệ.")
             )
           )
           (alert "Không có layer nào được chọn hoặc không có layer. Chọn chức năng khác.")
         )
        )
        ((= choice "dim_mleader")
         (delete-mleader)
        )
        ((= choice "dim_by_layer_select")
         (if (and layindex layerlist (> (length layerlist) 0) (/= layindex ""))
           (progn
             (setq layname (nth (atoi layindex) layerlist))
             (if (and layname (/= layname "Không có layer"))
               (delete-dim-by-layer-select layname)
               (alert "Layer không hợp lệ.")
             )
           )
           (alert "Không có layer nào được chọn hoặc không có layer. Chọn chức năng khác.")
         )
        )
        ((= choice "all_dim")
         (delete-all-dim)
        )
        (T
         (alert "Lựa chọn không hợp lệ. Mặc định xóa DIMENSION được chọn.")
         (delete-selected-dim)
        )
      )
    )
    (princ "\nĐã hủy lệnh.")
  )
  (princ)
)

; Function 1: Delete selected DIMENSION (old 4, now 1)
(defun delete-selected-dim (/ ss cnt)
  (princ "\nChọn các DIMENSION cần xóa (có thể quét chọn bằng window/crossing hoặc pick từng cái, Enter để kết thúc): ")
  (if (setq ss (ssget '((0 . "DIMENSION"))))
    (progn
      (setq cnt (sslength ss))
      (command "_.ERASE" ss "")
      (alert (strcat "Đã xóa " (itoa cnt) " DIMENSION được chọn."))
    )
    (alert "Không có DIMENSION nào được chọn.")
  )
  (princ)
)

; Function 2: Delete DIMENSION by layer (old 2, now 2)
(defun delete-dim-by-layer (layname / ss filter cnt)
  (setq filter (list '(0 . "DIMENSION") (cons 8 layname)))
  (if (setq ss (ssget "X" filter))
    (progn
      (setq cnt (sslength ss))
      (command "_.ERASE" ss "")
      (alert (strcat "Đã xóa " (itoa cnt) " DIMENSION trên layer " layname "."))
    )
    (alert (strcat "Không có DIMENSION nào trên layer " layname "."))
  )
)

; Function 3: Delete LEADER NOTE (MLEADER) (old 3, now 3)
(defun delete-mleader (/ ss cnt)
  (if (setq ss (ssget "X" '((0 . "MLEADER"))))
    (progn
      (setq cnt (sslength ss))
      (command "_.ERASE" ss "")
      (alert (strcat "Đã xóa " (itoa cnt) " LEADER NOTE (MLEADER)."))
    )
    (alert "Không có LEADER NOTE (MLEADER) nào trong bản vẽ.")
  )
)

; New Function 4: Delete DIMENSION by layer and selected area (based on 2 + selection like 1)
(defun delete-dim-by-layer-select (layname / filter ss cnt)
  (setq filter (list '(0 . "DIMENSION") (cons 8 layname)))
  (princ (strcat "\nChọn vùng DIMENSION trên layer " layname " (có thể quét chọn bằng window/crossing hoặc pick, Enter để kết thúc): "))
  (if (setq ss (ssget filter))
    (progn
      (setq cnt (sslength ss))
      (command "_.ERASE" ss "")
      (alert (strcat "Đã xóa " (itoa cnt) " DIMENSION trên layer " layname " trong vùng chọn."))
    )
    (alert (strcat "Không có DIMENSION nào được chọn trên layer " layname "."))
  )
  (princ)
)

; Function 5: Delete all DIMENSION (old 1, now 5)
(defun delete-all-dim (/ ss cnt)
  (if (setq ss (ssget "X" '((0 . "DIMENSION"))))
    (progn
      (setq cnt (sslength ss))
      (command "_.ERASE" ss "")
      (alert (strcat "Đã xóa " (itoa cnt) " DIMENSION."))
    )
    (alert "Không có DIMENSION nào trong bản vẽ.")
  )
)

(princ "\nGõ DIMDELETE để chạy lệnh. (Cập nhật: Thêm chức năng 4 - Xóa theo layer + vùng; Đổi thứ tự 1/4/5)")
(princ)