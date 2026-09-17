(vl-load-com)

(defun write-dcl-file (/ dcl-str f temp-path)
  (setq temp-path (strcat (getenv "TEMP") "\\changelayer.dcl"))
  (setq dcl-str
    "changelayer : dialog {
      label = \"Chuyển Layer\";
      width = 50;
      : column {
        : text { label = \"Chọn layer nguồn và đích:\"; alignment = centered; }
        : radio_column {
          key = \"mode\";
          : radio_button { key = \"all_mode\"; label = \"Chuyển tất cả trên layer nguồn\"; value = \"1\"; }
          : radio_button { key = \"select_mode\"; label = \"Chuyển entity được chọn trong vùng\"; }
        }
        : row {
          : column {
            : text { label = \"Layer nguồn:\"; alignment = left; }
            : row {
              : popup_list { key = \"sourcelist\"; width = 20; }
              : button { key = \"pick_source\"; label = \"Chọn Entity\"; width = 12; }
            }
          }
          : column {
            : text { label = \"Layer đích:\"; alignment = left; }
            : popup_list { key = \"targetlist\"; width = 20; }
          }
        }
      }
      ok_cancel;
    }"
  )
  (if (setq f (open temp-path "w"))
    (progn (write-line dcl-str f) (close f) temp-path)
    (progn (alert "Không thể tạo file DCL!") nil)
  )
)

(defun c:changelayer (/ dcl_id dcl-path layerlist lay layname sourceindex targetindex result mode entdata picked-layer idx *layerlist* sourcelayer targetlayer)
  (setq dcl-path (write-dcl-file))
  (if (not dcl-path) (exit))


(setq dcl-path (write-dcl-file))
(if (or (not dcl-path) (not (findfile dcl-path)))
  (progn
    (alert "Không thể tạo hoặc tìm thấy file DCL. Kiểm tra quyền thư mục TEMP.")
    (exit)
  )
)


  (setq dcl_id (load_dialog dcl-path))
  (if (not (new_dialog "changelayer" dcl_id))
    (progn (alert "Không load được hộp thoại.") (exit))
  )

  ;; Lấy danh sách layer (có cả layer 0)
  (setq layerlist '())
  (if (setq lay (tblnext "LAYER" T))
    (while lay
      (setq layname (cdr (assoc 2 lay)))
      (setq layerlist (cons layname layerlist))
      (setq lay (tblnext "LAYER"))
    )
  )
  (setq layerlist (reverse layerlist))

  ;; Điền danh sách
  (start_list "sourcelist")
  (foreach l layerlist (add_list l))
  (end_list)
  (set_tile "sourcelist" "0")

  (start_list "targetlist")
  (foreach l layerlist (add_list l))
  (end_list)
  (set_tile "targetlist" "0")

  (setq *layerlist* layerlist)
  (set_tile "all_mode" "1")

  ;; Khi nhấn “Chọn Entity”
  (action_tile "pick_source"
    "(done_dialog 2)"
  )

  ;; Khi nhấn OK
  (action_tile "accept"
    "(setq mode (get_tile \"mode\") sourceindex (get_tile \"sourcelist\") targetindex (get_tile \"targetlist\")) (done_dialog 1)"
  )
  (action_tile "cancel" "(done_dialog 0)")

  ;; Hiển thị hộp thoại
  (setq result (start_dialog))
  (unload_dialog dcl_id)
  (vl-file-delete dcl-path)

  ;; Xử lý kết quả
  (cond
    ((= result 0)
     (princ "\nĐã hủy lệnh.")
    )

    ;; Nếu chọn “Chọn Entity” → cập nhật layer nguồn rồi quay lại hộp thoại
    ((= result 2)
     (princ "\nChọn 1 đối tượng để lấy layer nguồn: ")
     (if (setq picked (entsel))
       (progn
         (setq ent (car picked))
         (setq entdata (entget ent))
         (setq picked-layer (cdr (assoc 8 entdata)))
         (if picked-layer
           (progn
             (alert (strcat "Đã chọn layer nguồn: " picked-layer))
             ;; Gọi lại chính lệnh với layer nguồn mặc định
             (c:changelayer-auto picked-layer)
           )
           (alert "Không lấy được layer từ entity.")
         )
       )
       (alert "Không chọn được đối tượng.")
     )
    )

    ;; Nếu nhấn OK
    ((= result 1)
     ;; Xác định layer nguồn và đích
     (if (and sourceindex layerlist)
       (setq sourcelayer (nth (atoi sourceindex) layerlist))
       (progn (alert "Không có layer nguồn nào được chọn.") (exit))
     )
     (if (and targetindex layerlist)
       (setq targetlayer (nth (atoi targetindex) layerlist))
       (progn (alert "Không có layer đích nào được chọn.") (exit))
     )

     (if (= sourcelayer targetlayer)
       (progn (alert "Layer nguồn và đích giống nhau, không cần chuyển.") (exit))
     )

     ;; Thực thi
     (cond
       ((= mode "all_mode")
        (change-all-entities sourcelayer targetlayer)
       )
       ((= mode "select_mode")
        (change-selected-entities-after-dialog sourcelayer targetlayer)
       )
       (T
        (alert "Chế độ không hợp lệ.")
       )
     )
    )
  )
  (princ)
)

;; Khi chọn entity để lấy layer nguồn xong, quay lại hộp thoại
(defun c:changelayer-auto (preset-layer / dcl_id dcl-path layerlist idx)
  (setq dcl-path (write-dcl-file))
  (setq dcl_id (load_dialog dcl-path))
  (if (not (new_dialog "changelayer" dcl_id))
    (progn (alert "Không load được hộp thoại.") (exit))
  )

  ;; Load lại layer list
  (setq layerlist '())
  (if (setq lay (tblnext "LAYER" T))
    (while lay
      (setq layname (cdr (assoc 2 lay)))
      (setq layerlist (cons layname layerlist))
      (setq lay (tblnext "LAYER"))
    )
  )
  (setq layerlist (reverse layerlist))

  ;; Điền danh sách
  (start_list "sourcelist")
  (foreach l layerlist (add_list l))
  (end_list)
  (if (setq idx (vl-position preset-layer layerlist))
    (set_tile "sourcelist" (itoa idx))
  )

  (start_list "targetlist")
  (foreach l layerlist (add_list l))
  (end_list)
  (set_tile "targetlist" "0")

  (setq *layerlist* layerlist)
  (set_tile "all_mode" "1")

  (action_tile "pick_source"
    "(done_dialog 2)"
  )
  (action_tile "accept"
    "(setq mode (get_tile \"mode\") sourceindex (get_tile \"sourcelist\") targetindex (get_tile \"targetlist\")) (done_dialog 1)"
  )
  (action_tile "cancel" "(done_dialog 0)")

  (setq result (start_dialog))
  (unload_dialog dcl_id)
  (vl-file-delete dcl-path)

  (cond
    ((= result 2) (c:changelayer))
    ((= result 1)
     (setq sourcelayer (nth (atoi sourceindex) layerlist))
     (setq targetlayer (nth (atoi targetindex) layerlist))
     (if (= sourcelayer targetlayer)
       (alert "Layer nguồn và đích giống nhau.")
       (if (= mode "all_mode")
         (change-all-entities sourcelayer targetlayer)
         (change-selected-entities-after-dialog sourcelayer targetlayer)
       )
     )
    )
  )
)

;; === CHỨC NĂNG CHÍNH ===
(defun change-all-entities (sourcelayer targetlayer / ss cnt)
  (if (setq ss (ssget "X" (list (cons 8 sourcelayer))))
    (progn
      (setq cnt (sslength ss))
      (command "_.CHPROP" ss "" "_LA" targetlayer "")
      (alert (strcat "Đã chuyển " (itoa cnt) " entity từ layer " sourcelayer " sang " targetlayer "."))
    )
    (alert (strcat "Không có entity nào trên layer " sourcelayer "."))
  )
)

(defun change-selected-entities-after-dialog (sourcelayer targetlayer / ss cnt)
  (princ (strcat "\nChọn vùng entity trên layer " sourcelayer " (Enter để kết thúc chọn): "))
  (if (setq ss (ssget (list (cons 8 sourcelayer))))
    (progn
      (setq cnt (sslength ss))
      (command "_.CHPROP" ss "" "_LA" targetlayer "")
      (alert (strcat "Đã chuyển " (itoa cnt) " entity từ layer " sourcelayer " sang " targetlayer " trong vùng chọn."))
    )
    (alert "Không có entity nào được chọn trên layer nguồn.")
  )
)

(princ "\nGõ CHANGELAYER để chạy lệnh.")
(princ)
