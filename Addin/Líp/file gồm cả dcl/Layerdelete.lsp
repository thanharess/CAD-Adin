(vl-load-com)

(defun c:layerdelete (/ dcl_id dcl-path layerlist lay layname layindex choice result *layerlist* picked entdata picked-layer idx)
  (setq dcl-path "layerdelete.dcl")
  (if (or (not (findfile dcl-path)))
    (progn
      (alert "Không thể tìm thấy file DCL 'layerdelete.dcl' trong thư mục hiện tại. Đặt file cùng folder với LISP.")
      (exit)
    )
  )
  (setq dcl_id (load_dialog dcl-path))
  (if (not (new_dialog "layerdel" dcl_id))
    (progn (alert "Không load được hộp thoại.") (exit))
  )
  ;; Lấy danh sách layer
  (setq layerlist '())
  (if (setq lay (tblnext "LAYER" T))
    (while lay
      (setq layname (cdr (assoc 2 lay)))
      (if (not (equal layname "Defpoints"))
        (setq layerlist (cons layname layerlist))
      )
      (setq lay (tblnext "LAYER"))
    )
  )
  (setq layerlist (reverse layerlist))
  (setq *layerlist* layerlist)
  ;; Đổ dữ liệu vào popup list
  (start_list "layerlist")
  (foreach l layerlist (add_list l))
  (end_list)
  ;; Nếu trước đó đã chọn entity thì tự cập nhật vào ô layerlist
  (if (and (boundp '*picked-layer*) *picked-layer*)
    (if (setq idx (vl-position *picked-layer* *layerlist*))
      (set_tile "layerlist" (itoa idx))
      (set_tile "layerlist" "0")
    )
    (set_tile "layerlist" "0")
  )
  ;; Nút chọn Entity
  (action_tile "pick_layer"
    "(progn
       (done_dialog 2)
     )"
  )
  ;; Khi nhấn OK
  (action_tile "accept"
    "(setq choice (get_tile \"options\") layindex (get_tile \"layerlist\")) (done_dialog 1)"
  )
  (action_tile "cancel" "(done_dialog 0)")
  ;; Chạy dialog
  (setq result (start_dialog))
  (unload_dialog dcl_id)
  ;; Nếu nhấn nút “Chọn Entity”
  (if (= result 2)
    (progn
      (prompt "\nChọn 1 đối tượng để lấy layer: ")
      (if (setq picked (entsel))
        (progn
          (setq entdata (entget (car picked)))
          (setq picked-layer (cdr (assoc 8 entdata)))
          (if picked-layer
            (progn
              (setq *picked-layer* picked-layer)
              (alert (strcat "Đã chọn layer: " picked-layer))
              (c:layerdelete) ;; Gọi lại dialog và giữ layer đã chọn
            )
            (progn
              (alert "Không lấy được layer từ entity.")
              (c:layerdelete)
            )
          )
        )
        (progn
          (alert "Không chọn được đối tượng.")
          (c:layerdelete)
        )
      )
      (exit)
    )
  )
  ;; Nếu nhấn Cancel
  (if (/= result 1)
    (progn (princ "\nĐã hủy lệnh.") (exit))
  )
  ;; Lấy layer đã chọn
  (if (and layindex (numberp (atoi layindex)) layerlist (> (length layerlist) 0))
    (setq layname (nth (atoi layindex) layerlist))
    (progn (alert "Không có layer nào được chọn.") (exit))
  )
  ;; Xử lý chức năng
  (cond
    ((= choice "dim_by_layer_select")
     (delete-entities-by-layer-select layname)
    )
    ((= choice "dim_by_layer")
     (delete-entities-by-layer layname)
    )
    ((= choice "all_dim")
     (purge-all-unused-layers)
    )
    (T (alert "Lựa chọn không hợp lệ."))
  )
  (princ)
)

;; Xóa entity trên layer theo vùng chọn
(defun delete-entities-by-layer-select (layname / ss cnt)
  (if (not layname) (setq layname ""))
  (princ (strcat "\nChọn vùng các entity trên layer " layname ": "))
  (setq ss (ssget (list (cons 8 layname))))
  (if ss
    (progn
      (setq cnt (sslength ss))
      (command "_.ERASE" ss "")
      (alert (strcat "Đã xóa " (itoa cnt) " entity trên layer " layname " trong vùng chọn."))
    )
    (alert "Không có entity nào được chọn.")
  )
)

;; Xóa toàn bộ entity trên layer
(defun delete-entities-by-layer (layname / ss cnt)
  (if (not layname) (setq layname ""))
  (setq ss (ssget "X" (list (cons 8 layname))))
  (if ss
    (progn
      (setq cnt (sslength ss))
      (command "_.ERASE" ss "")
      (alert (strcat "Đã xóa " (itoa cnt) " entity trên layer " layname "."))
    )
    (alert (strcat "Không có entity nào trên layer " layname "."))
  )
)

;; Purge tất cả layer không dùng
(defun purge-all-unused-layers ()
  (command "_.PURGE" "_LA" "*" "N")
  (alert "Đã purge tất cả layer không dùng.")
)

(princ "\nGõ LAYERDELETE để chạy lệnh.")
(princ)